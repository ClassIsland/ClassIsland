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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class ScheduleReminderService : IHostedService, IDisposable
{
    private readonly IProfileService _profileService;
    private readonly ILogger<ScheduleReminderService> _logger;
    private readonly IEnumerable<IHostedService> _hostedServices;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(30);
    private Timer? _timer;
    private bool _running = false;

    public ScheduleReminderService(IProfileService profileService, ILogger<ScheduleReminderService> logger, IEnumerable<IHostedService> hostedServices)
    {
        _profileService = profileService;
        _logger = logger;
        _hostedServices = hostedServices;
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
                var reminders = _profileService.Profile.Reminders.ToList();
                _logger.LogDebug("检查提醒：共 {0} 条", reminders.Count);
            foreach (var rem in reminders)
            {
                    _logger.LogDebug("提醒条目: Id={Id} Title='{Title}' Enabled={Enabled} StoredTime={Time}", rem.Id, rem.Title, rem.IsEnabled, rem.Time);
                if (!rem.IsEnabled) continue;
                    var from = now - _pollingInterval;
                    var next = rem.GetNextOccurrence(from);
                    _logger.LogDebug("计算下次发生: {0} (from={1})", next?.ToString() ?? "<null>", from);
                if (next != null && next <= now)
                {
                    try
                    {
                        _logger.LogInformation("触发提醒：{0} @ {1}", rem.Title, next);
                        var request = new NotificationRequest
                        {
                            MaskContent = ClassIsland.Core.Models.Notification.NotificationContent.CreateTwoIconsMask(rem.Title, hasRightIcon: false),
                            OverlayContent = string.IsNullOrEmpty(rem.Message) ? null : ClassIsland.Core.Models.Notification.NotificationContent.CreateSimpleTextContent(rem.Message),
                        };

                        var provider = _hostedServices.OfType<ActionNotificationProvider>().FirstOrDefault();
                        if (provider == null)
                        {
                            _logger.LogWarning("未找到 ActionNotificationProvider 实例，无法显示提醒");
                        }
                        else
                        {
                            _logger.LogDebug("使用 ActionNotificationProvider 显示提醒");
                            await provider.ShowNotificationAsync(request);
                        }

                        rem.AdvanceNextOccurrence();
                        _profileService.SaveProfile();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "发出提醒时发生错误：{0}", rem.Title);
                    }
                }
            }
        }
        finally
        {
            _running = false;
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
