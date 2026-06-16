using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Services.NotificationProviders;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Shared;
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
    private Timer? _timer;
    private bool _running = false;

    // 缓存已解析的 Provider（懒加载），避免重复查找
    private ReminderNotificationProvider? _cachedProvider;

    // 窗口式检查：记录上次检查时间
    private DateTime _lastCheckTime;

    // 已触发过的 (ReminderId, TriggerDateTime) 去重集合，避免同一次调度中重复触发
    private readonly HashSet<(Guid ReminderId, DateTime TriggerTime)> _triggeredReminders = new();

    // 标记时钟滴答间隔（固定 5 秒），CheckReminders 内部根据配置间隔决定是否真正执行
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);

    // 最长追赶窗口：防止休眠后大量历史提醒涌出
    private static readonly TimeSpan MaxCatchUpWindow = TimeSpan.FromHours(6);

    // 清理去重记录：仅保留最近 N 分钟内的条目
    private static readonly TimeSpan DedupRetention = TimeSpan.FromMinutes(10);

    private DateTime _lastDedupCleanup = DateTime.MinValue;

    public ScheduleReminderService(
        IProfileService profileService,
        ILogger<ScheduleReminderService> logger,
        IServiceProvider serviceProvider)
    {
        _profileService = profileService;
        _logger = logger;
        _serviceProvider = serviceProvider;
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

    private async Task CheckReminders()
    {
        if (_running) return;
        _running = true;
        try
        {
            // --- 1. 运行时解析 Provider ---
            var provider = ResolveProvider();
            if (provider == null)
                return;

            var settings = provider.Settings;

            // --- 2. 检查 Provider 是否启用 ---
            if (!settings.IsEnabled)
            {
                _lastCheckTime = DateTime.Now; // 即使禁用也更新检查点，避免积压
                return;
            }

            var interval = TimeSpan.FromSeconds(settings.CheckIntervalSeconds);
            var now = DateTime.Now;

            // --- 3. 判断是否到了执行间隔 ---
            var elapsed = now - _lastCheckTime;
            if (elapsed < interval)
                return; // 间隔未到，跳过

            // --- 4. 计算检查窗口并更新检查点 ---
            var windowStart = _lastCheckTime;
            // 如果窗口过大，限制追赶范围，防止休眠后大量提醒涌出
            if (elapsed > MaxCatchUpWindow)
                windowStart = now - MaxCatchUpWindow;
            _lastCheckTime = now;

            _logger.LogDebug("日程提醒检查窗口 [{0} - {1}] 间隔={2}s",
                windowStart.ToString("HH:mm:ss"), now.ToString("HH:mm:ss"), interval.TotalSeconds);

            var reminders = _profileService.Profile.Reminders.ToList();
            if (reminders.Count == 0)
                return;

            foreach (var rem in reminders)
            {
                if (!rem.IsEnabled) continue;

                // 查找窗口内所有可能的触发时刻
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
                        _logger.LogInformation("触发日程提醒：{0} @ {1:HH:mm:ss}", rem.Title, triggerTime);

                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            var request = new NotificationRequest
                            {
                                MaskContent = ClassIsland.Core.Models.Notification.NotificationContent.CreateTwoIconsMask(rem.Title, hasRightIcon: false),
                                OverlayContent = string.IsNullOrEmpty(rem.Message) ? null : ClassIsland.Core.Models.Notification.NotificationContent.CreateSimpleTextContent(rem.Message),
                            };

                            _logger.LogDebug("使用 ReminderNotificationProvider 显示提醒");
                            await provider.ShowNotificationAsync(request).ConfigureAwait(false);

                            // 推进下次发生时间并持久化
                            rem.AdvanceNextOccurrence();
                            _profileService.SaveProfile();
                        }).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "发出日程提醒时发生错误：{0}", rem.Title);
                    }
                }
            }

            // --- 5. 定期清理去重记录 ---
            CleanupDedupSet(now);
        }
        finally
        {
            _running = false;
        }
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
