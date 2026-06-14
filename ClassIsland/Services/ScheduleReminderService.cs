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
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(30);
    private Timer? _timer;
    private bool _running = false;

    // 用于在相同时:分内防止重复触发（只比较时和分，不考虑秒）
    private readonly HashSet<(int Hour, int Minute, Guid ReminderId)> _triggeredInCurrentMinute = new();
    private int _lastCheckedHour = -1;
    private int _lastCheckedMinute = -1;

    public ScheduleReminderService(IProfileService profileService, ILogger<ScheduleReminderService> logger, IServiceProvider serviceProvider)
    {
        _profileService = profileService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ScheduleReminderService starting.");
        // 检查周期：30 秒
        _timer = new Timer(async _ => await CheckReminders(), null, TimeSpan.Zero, _pollingInterval);
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
            var now = DateTime.Now;

            // 跨分钟时清空触发记录
            if (now.Hour != _lastCheckedHour || now.Minute != _lastCheckedMinute)
            {
                _triggeredInCurrentMinute.Clear();
                _lastCheckedHour = now.Hour;
                _lastCheckedMinute = now.Minute;
            }

            var reminders = _profileService.Profile.Reminders.ToList();
            _logger.LogDebug("检查提醒：共 {0} 条", reminders.Count);

            foreach (var rem in reminders)
            {
                _logger.LogDebug("提醒条目: Id={Id} Title='{Title}' Enabled={Enabled} TimeOfDay={Time}",
                    rem.Id, rem.Title, rem.IsEnabled, rem.TimeOfDay);
                if (!rem.IsEnabled) continue;

                // 只比较小时和分钟，不考虑秒
                if (rem.TimeOfDay.Hours != now.Hour || rem.TimeOfDay.Minutes != now.Minute)
                    continue;

                // 检查是否已经在当前时:分触发过（同一分钟防重复）
                var key = (now.Hour, now.Minute, rem.Id);
                if (_triggeredInCurrentMinute.Contains(key))
                {
                    _logger.LogDebug("提醒 {0} 已在当前时:分触发过，跳过", rem.Title);
                    continue;
                }

                // 检查日期范围
                if (rem.StartDate.HasValue && now.Date < rem.StartDate.Value.Date) continue;
                if (rem.EndDate.HasValue && now.Date > rem.EndDate.Value.Date) continue;

                // 频率特定检查
                switch (rem.Frequency)
                {
                    case ReminderFrequency.Once:
                        // Once 类型：日期必须完全匹配
                        if (rem.Time.Date != now.Date) continue;
                        break;
                    case ReminderFrequency.Weekly:
                        // Weekly 类型：今天必须在选中的星期中
                        var flag = DayOfWeekToFlag(now.DayOfWeek);
                        if (!rem.WeekDays.HasFlag(flag)) continue;
                        break;
                    case ReminderFrequency.Yearly:
                        // Yearly 类型：月/日必须匹配
                        var month = rem.YearMonth > 0 ? rem.YearMonth : rem.Time.Month;
                        var day = rem.YearDay > 0 ? rem.YearDay : rem.Time.Day;
                        if (now.Month != month || now.Day != day) continue;
                        break;
                    // Daily: 无需额外检查（日期范围已在上面检查）
                }

                _triggeredInCurrentMinute.Add(key);

                try
                {
                    _logger.LogInformation("触发提醒：{0} @ {1}:{2:D2}", rem.Title, now.Hour, now.Minute);

                    // Create and show notification on the UI thread because building
                    // NotificationContent may touch Avalonia objects (icons, controls,
                    // etc.) which must be constructed on the UI dispatcher.
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        var request = new NotificationRequest
                        {
                            MaskContent = ClassIsland.Core.Models.Notification.NotificationContent.CreateTwoIconsMask(rem.Title, hasRightIcon: false),
                            OverlayContent = string.IsNullOrEmpty(rem.Message) ? null : ClassIsland.Core.Models.Notification.NotificationContent.CreateSimpleTextContent(rem.Message),
                        };

                        var provider = _serviceProvider.GetServices<IHostedService>().OfType<ActionNotificationProvider>().FirstOrDefault();
                        if (provider == null)
                        {
                            _logger.LogWarning("未找到 ActionNotificationProvider 实例，无法显示提醒");
                        }
                        else
                        {
                            _logger.LogDebug("使用 ActionNotificationProvider 显示提醒");
                            await provider.ShowNotificationAsync(request).ConfigureAwait(false);
                        }

                        // Advance occurrence and persist profile while still on UI thread
                        // to avoid races with UI-bound reminder state.
                        rem.AdvanceNextOccurrence();
                        _profileService.SaveProfile();
                    }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "发出提醒时发生错误：{0}", rem.Title);
                }
            }
        }
        finally
        {
            _running = false;
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

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
