using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Core.Models.Ruleset;
using ClassIsland.Models.NotificationProviderSettings;
using ClassIsland.Services.NotificationProviders;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Automation;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class ScheduleReminderService : IHostedService, IDisposable
{
    private readonly IProfileService _profileService;
    private readonly ILogger<ScheduleReminderService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IActionService? _actionService;
    private readonly IRulesetService? _rulesetService;
    private Timer? _timer;
    private bool _running = false;

    // 缓存已解析的 Provider（懒加载），避免重复查找
    private ReminderNotificationProvider? _cachedProvider;

    // 窗口式检查：记录上次检查时间
    private DateTime _lastCheckTime;

    // 已触发过的 (ReminderId, TriggerDateTime) 去重集合，避免同一次调度中重复触发
    private readonly HashSet<(Guid ReminderId, DateTime TriggerTime)> _triggeredReminders = new();

    // 时钟滴答间隔（固定 5 秒）。窗口模式由 CheckIntervalSeconds 节流，
    // 精确模式（分钟精度）由 _lastPreciseCheckTime 节流。
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);

    // 精确模式（分钟精度）：检查间隔，固定 60 秒
    private static readonly TimeSpan PreciseCheckInterval = TimeSpan.FromSeconds(60);

    // 待保存标记：每个提醒触发时不立即写入，而在本轮检查结束时统一写入
    private bool _pendingSave = false;

    // 最长追赶窗口：防止休眠后大量历史提醒涌出
    private static readonly TimeSpan MaxCatchUpWindow = TimeSpan.FromHours(6);

    // 清理去重记录：仅保留最近 N 分钟内的条目
    private static readonly TimeSpan DedupRetention = TimeSpan.FromMinutes(10);

    private DateTime _lastDedupCleanup = DateTime.MinValue;

    // 精确模式（分钟精度）：记录上次检查时间，用于 60 秒节流
    private DateTime _lastPreciseCheckTime = DateTime.MinValue;

    public ScheduleReminderService(
        IProfileService profileService,
        ILogger<ScheduleReminderService> logger,
        IServiceProvider serviceProvider,
        IActionService actionService,
        IRulesetService rulesetService)
    {
        _profileService = profileService;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _actionService = actionService;
        _rulesetService = rulesetService;
    }

    /// <summary>
    /// 运行时获取 ReminderNotificationProvider 实例（懒加载 + 缓存）。
    /// </summary>
    private ReminderNotificationProvider? ResolveProvider()
    {
        if (_cachedProvider != null)
            return _cachedProvider;

        _cachedProvider = _serviceProvider.GetServices<IHostedService>()
            .OfType<ReminderNotificationProvider>()
            .FirstOrDefault();

        if (_cachedProvider == null)
            _logger.LogWarning("未找到 ReminderNotificationProvider 实例");

        return _cachedProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ScheduleReminderService starting.");
        _lastCheckTime = DateTime.Now;
        _timer = new Timer(async _ => await CheckReminders(), null, TimeSpan.Zero, TickInterval);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ScheduleReminderService stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 请求立即检查一次日程提醒。编辑日程后调用，确保新设定的日程能及时触发。
    /// </summary>
    public void RequestImmediateCheck()
    {
        _lastPreciseCheckTime = DateTime.MinValue;
        _lastCheckTime = DateTime.MinValue;
        _logger.LogDebug("收到立即检查请求，将在下次滴答时执行检查");
    }

    private async Task CheckReminders()
    {
        if (_running) return;
        _running = true;
        try
        {
            var provider = ResolveProvider();
            FlushPendingSave();
            if (provider == null)
                return;

            var settings = provider.Settings;
            if (!settings.IsEnabled)
            {
                _lastCheckTime = DateTime.Now;
                return;
            }

            var now = DateTime.Now;

            // 根据 CheckMode 选择检查策略
            if (settings.CheckMode == ReminderCheckMode.WindowBased)
            {
                await CheckRemindersWindowBased(now, settings, provider);
            }
            else
            {
                await CheckRemindersPrecise(now, provider);
            }

            CleanupDedupSet(now);
            FlushPendingSave();
        }
        finally
        {
            _running = false;
        }
    }

    /// <summary>
    /// 将待处理的 Profile 写入刷入磁盘。
    /// 将多次触发时的写入请求合并为一次，避免频繁完整写入整个档案。
    /// </summary>
    private void FlushPendingSave()
    {
        if (!_pendingSave) return;
        _pendingSave = false;
        _profileService.SaveProfile();
    }

    private async Task CheckRemindersWindowBased(DateTime now, ReminderNotificationProviderSettings settings, ReminderNotificationProvider provider)
    {
        var interval = TimeSpan.FromSeconds(settings.CheckIntervalSeconds);
        var elapsed = now - _lastCheckTime;
        if (elapsed < interval)
            return; // 间隔未到，跳过

        var windowStart = _lastCheckTime;
        if (elapsed > MaxCatchUpWindow)
            windowStart = now - MaxCatchUpWindow;
        _lastCheckTime = now;

        _logger.LogDebug("日程提醒窗口检查 [{0:HH:mm:ss.fff}] 窗口=[{1:HH:mm:ss.fff} - {2:HH:mm:ss.fff}] 间隔={3}s",
            now, windowStart, now, interval.TotalSeconds);

        var reminders = _profileService.Profile.Reminders.ToList();
        if (reminders.Count == 0)
            return;

        foreach (var rem in reminders)
        {
            if (!rem.IsEnabled) continue;

            // 在时间窗口内查找所有可能的触发时刻
            var candidates = GetTriggerCandidatesInWindow(rem, windowStart, now);
            foreach (var triggerTime in candidates)
            {
                var dedupKey = (rem.Id, triggerTime);
                if (_triggeredReminders.Contains(dedupKey))
                {
                    _logger.LogTrace("日程 [{Id}] '{Title}' 在 {t} 已触发过，跳过", rem.Id, rem.Title, triggerTime);
                    continue;
                }

                _triggeredReminders.Add(dedupKey);

                try
                {
                    await ProcessReminderTrigger(rem, triggerTime, provider);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "发出日程提醒时发生错误：{0}", rem.Title);
                }
            }
        }
    }

    private async Task CheckRemindersPrecise(DateTime now, ReminderNotificationProvider provider)
    {
        // 当前分钟的起止时间（精确到分）
        var minuteStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

        // 固定 60 秒检查一次，精确到分钟
        if (now - _lastPreciseCheckTime < PreciseCheckInterval)
            return;
        _lastPreciseCheckTime = minuteStart; // 存整分，确保下一分钟一定触发

        var windowStart = _lastCheckTime;
        var elapsed = now - windowStart;
        if (elapsed > MaxCatchUpWindow)
            windowStart = now - MaxCatchUpWindow;
        _lastCheckTime = now;

        var minuteEnd = minuteStart.AddMinutes(1);

        _logger.LogDebug("日程提醒分钟精确检查 [{0:HH:mm}] 窗口=[{1:HH:mm:ss} - {2:HH:mm:ss}]",
            now, windowStart, now);

        var reminders = _profileService.Profile.Reminders.ToList();
        if (reminders.Count == 0)
            return;

        foreach (var rem in reminders)
        {
            if (!rem.IsEnabled) continue;

            var nextTime = rem.GetNextOccurrence(windowStart);
            if (nextTime == null) continue;

            // 精确到分钟：触发时间必须在当前分钟内
            if (nextTime.Value < minuteStart || nextTime.Value >= minuteEnd)
                continue;

            var dedupKey = (rem.Id, nextTime.Value);
            if (_triggeredReminders.Contains(dedupKey))
            {
                _logger.LogTrace("日程 [{Id}] '{Title}' 在 {t} 已触发过，跳过", rem.Id, rem.Title, nextTime.Value);
                continue;
            }

            _triggeredReminders.Add(dedupKey);

            try
            {
                await ProcessReminderTrigger(rem, nextTime.Value, provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发出日程提醒时发生错误：{0}", rem.Title);
            }
        }
    }

    private async Task ProcessReminderTrigger(Reminder rem, DateTime triggerTime, ReminderNotificationProvider provider)
    {
        _logger.LogInformation("触发日程提醒：{0} @ {1:HH:mm:ss.fff}", rem.Title, triggerTime);

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var request = new NotificationRequest
            {
                MaskContent = ClassIsland.Core.Models.Notification.NotificationContent.CreateTwoIconsMask(rem.Title, hasRightIcon: false),
                OverlayContent = string.IsNullOrEmpty(rem.Message) ? null : ClassIsland.Core.Models.Notification.NotificationContent.CreateSimpleTextContent(rem.Message),
            };

            _logger.LogDebug("使用 ReminderNotificationProvider 显示提醒");
            await provider.ShowNotificationAsync(request).ConfigureAwait(false);

            // 检查条件
            var shouldExecuteActions = true;
            if (rem.IsConditionEnabled && rem.ConditionSettings != null && _rulesetService != null)
            {
                try
                {
                    var json = JsonSerializer.Serialize(rem.ConditionSettings);
                    var ruleset = JsonSerializer.Deserialize<Ruleset>(json);
                    if (ruleset != null)
                    {
                        shouldExecuteActions = _rulesetService.IsRulesetSatisfied(ruleset);
                        if (!shouldExecuteActions)
                            _logger.LogInformation("日程提醒的条件不满足，跳过执行自动化行动：{0}", rem.Title);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "解析日程提醒的条件规则集时出错，将执行自动化行动：{0}", rem.Title);
                }
            }

            if (shouldExecuteActions && rem.ActionSet is { ActionItems.Count: > 0 } actionSet && _actionService != null)
            {
                _logger.LogInformation("执行日程提醒关联的自动化行动组：{0}", rem.Title);
                await _actionService.InvokeActionSetAsync(actionSet, isRevertable: false).ConfigureAwait(false);
            }

            rem.AdvanceNextOccurrence();
            _pendingSave = true;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// 获取给定 Reminder 在 [windowStart, windowEnd] 窗口内的所有有效触发时刻。
    /// </summary>
    private static IEnumerable<DateTime> GetTriggerCandidatesInWindow(Reminder rem, DateTime windowStart, DateTime windowEnd)
    {
        if (rem.Frequency == ReminderFrequency.Once)
        {
            // 一次性提醒：使用 rem.Time 的精确时间
            if (rem.Time >= windowStart && rem.Time <= windowEnd)
                yield return rem.Time;
            yield break;
        }

        // 限制天数循环范围，避免极端情况
        var dayCount = (windowEnd.Date - windowStart.Date).Days + 1;
        if (dayCount > 7)
            dayCount = 7; // 最多检查 7 天

        for (var i = 0; i < dayCount; i++)
        {
            var day = windowStart.Date.AddDays(i);
            var candidate = day + rem.TimeOfDay;

            if (candidate < windowStart || candidate > windowEnd)
                continue;

            // 日期范围检查
            if (rem.StartDate.HasValue && day < rem.StartDate.Value.Date) continue;
            if (rem.EndDate.HasValue && day > rem.EndDate.Value.Date) continue;

            // 频率检查
            switch (rem.Frequency)
            {
                case ReminderFrequency.Daily:
                    break; // 每天都可以
                case ReminderFrequency.Weekly:
                    if (!rem.WeekDays.HasFlag(DayOfWeekToFlag(day.DayOfWeek)))
                        continue;
                    break;
                case ReminderFrequency.Yearly:
                {
                    var month = rem.YearMonth > 0 ? rem.YearMonth : rem.Time.Month;
                    var monthDay = rem.YearDay > 0 ? rem.YearDay : rem.Time.Day;
                    if (day.Month != month || day.Day != monthDay)
                        continue;
                    break;
                }
                default:
                    continue;
            }

            yield return candidate;
        }
    }

    private static ReminderWeekDays DayOfWeekToFlag(DayOfWeek dow) => dow switch
    {
        DayOfWeek.Sunday => ReminderWeekDays.Sunday,
        DayOfWeek.Monday => ReminderWeekDays.Monday,
        DayOfWeek.Tuesday => ReminderWeekDays.Tuesday,
        DayOfWeek.Wednesday => ReminderWeekDays.Wednesday,
        DayOfWeek.Thursday => ReminderWeekDays.Thursday,
        DayOfWeek.Friday => ReminderWeekDays.Friday,
        DayOfWeek.Saturday => ReminderWeekDays.Saturday,
        _ => ReminderWeekDays.None
    };

    /// <summary>
    /// 定期清理去重集合中过老的条目。
    /// </summary>
    private void CleanupDedupSet(DateTime now)
    {
        if ((now - _lastDedupCleanup).TotalMinutes < 2)
            return;
        _lastDedupCleanup = now;

        var threshold = now - DedupRetention;
        var toRemove = _triggeredReminders
            .Where(kvp => kvp.TriggerTime < threshold)
            .ToList();
        foreach (var key in toRemove)
            _triggeredReminders.Remove(key);

        if (toRemove.Count > 0)
            _logger.LogTrace("清理去重记录 {Count} 条", toRemove.Count);
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}