#if false
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Controls.NotificationProviders;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models.ActionSettings;
using ClassIsland.Shared.Abstraction.Models;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Models.AttachedSettings;
using ClassIsland.Models.NotificationProviderSettings;
using ClassIsland.Shared;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Helpers;

namespace ClassIsland.Services.NotificationProviders;

[NotificationProviderInfo("7625DE96-38AA-4B71-B478-3F156DD9458D", "天气预警", MaterialIconKind.CloudWarning, "当有降雨或者极端天气时发出提醒。")]
public class WeatherNotificationProvider : NotificationProviderBase<WeatherNotificationProviderSettings>
{

    private IWeatherService WeatherService { get; }

    private SettingsService SettingsService { get; }

    private INotificationHostService NotificationHostService { get; }

    private IAttachedSettingsHostService AttachedSettingsHostService { get; }

    private ILessonsService LessonsService { get; }
    public IActionService ActionService { get; }

    private List<string> ShownAlerts { get; } = new();

    public WeatherNotificationProvider(INotificationHostService notificationHostService,
        IAttachedSettingsHostService attachedSettingsHostService,
        IWeatherService weatherService,
        SettingsService settingsService,
        ILessonsService lessonsService,
        IActionService actionService)
    {
        NotificationHostService = notificationHostService;
        WeatherService = weatherService;
        SettingsService = settingsService;
        AttachedSettingsHostService = attachedSettingsHostService;
        LessonsService = lessonsService;
        ActionService = actionService;

        LessonsService.OnBreakingTime += NotificationHostServiceOnOnBreakingTime;
        LessonsService.OnClass += NotificationHostServiceOnOnClass;

        ActionService.RegisterActionHandler("classisland.notification.weather", (settings, _) =>
            AppBase.Current.Dispatcher.Invoke(() => HandleWeatherAction(settings)));
    }

    private void HandleWeatherAction(object? s)
    {
        if (s is not WeatherNotificationActionSettings settings)
        {
            return;
        }

        switch (settings.NotificationKind)
        {
            case 0:
                ShowWeatherForecastCore();
                break;
            case 1:
                ShowAlertsNotificationCore();
                break;
            case 2:
                ShowWeatherForecastHourlyCore();
                break;
        }
    }

    private void NotificationHostServiceOnOnClass(object? sender, EventArgs e)
    {
        ShowAlertsNotification();
        ShowForecastNotification();
    }

    private void ShowForecastNotification()
    {
        if (!WeatherService.IsWeatherRefreshed)
            return;
        var s = (IWeatherNotificationSettingsBase?)IAttachedSettingsHostService
            .GetAttachedSettingsByPriority<WeatherNotificationAttachedSettings>(ProviderGuid,
                timeLayoutItem: LessonsService.CurrentTimeLayoutItem) ?? Settings;
        if (!Settings.IsForecastEnabled || s.ForecastShowMode == NotificationModes.Disabled ||
            (s.ForecastShowMode == NotificationModes.Default &&
             Settings.ForecastShowMode == NotificationModes.Disabled))
        {
            return;
        }
        ShowWeatherForecastCore();
    }

    private void ShowWeatherForecastCore()
    {
        ShowNotification(new NotificationRequest()
        {
            MaskContent = new NotificationContent(new WeatherForecastNotificationProvider(true, SettingsService.Settings.LastWeatherInfo)),
            OverlayContent = new NotificationContent(new WeatherForecastNotificationProvider(false, SettingsService.Settings.LastWeatherInfo))
            {
                Duration = TimeSpan.FromSeconds(15)
            }
        });
    }

    private static DateTime RoundUpToHour(DateTime dateTime)
    {
        var ticksInHour = TimeSpan.TicksPerHour; // 每小时的Ticks数（3600,000,0000）
        var remainder = dateTime.Ticks % ticksInHour;
        return remainder == 0 ? dateTime : dateTime.AddTicks(ticksInHour - remainder);
    }

    private void ShowWeatherForecastHourlyCore()
    {
        var baseTime = SettingsService.Settings.LastWeatherInfo.UpdateTime;
        baseTime = RoundUpToHour(baseTime);
        ShowNotification(new NotificationRequest()
        {
            MaskContent = new NotificationContent(new WeatherHourlyForecastNotificationProvider(true, SettingsService.Settings.LastWeatherInfo, baseTime)),
            OverlayContent = new NotificationContent(new WeatherHourlyForecastNotificationProvider(false, SettingsService.Settings.LastWeatherInfo, baseTime))
            {
                Duration = TimeSpan.FromSeconds(15)
            }
        });
    }

    private void NotificationHostServiceOnOnBreakingTime(object? sender, EventArgs e)
    {
        ShowAlertsNotification();
        ShowForecastNotification();
    }

    private void ShowAlertsNotification()
    {
        if (!WeatherService.IsWeatherRefreshed)
            return;
        var s = (IWeatherNotificationSettingsBase?)IAttachedSettingsHostService
            .GetAttachedSettingsByPriority<WeatherNotificationAttachedSettings>(ProviderGuid,
                timeLayoutItem: LessonsService.CurrentTimeLayoutItem) ?? Settings;
        if (!Settings.IsAlertEnabled || s.AlertShowMode == NotificationModes.Disabled ||
            (s.AlertShowMode == NotificationModes.Default &&
             Settings.AlertShowMode == NotificationModes.Disabled))
        {
            return;
        }

        ShowAlertsNotificationCore();
    }

    private void ShowAlertsNotificationCore()
    {
        foreach (var i in SettingsService.Settings.LastWeatherInfo.Alerts.Where(i => !ShownAlerts.Contains(i.Detail)))
        {
            var t = i.Detail.Length / Settings.WeatherAlertSpeed;
            if (t <= 10) t = 10.0;
            if (t >= 90) t = 90.0;
            var ts = TimeSpanHelper.FromSecondsSafe(t);
            IAppHost.GetService<ILogger<WeatherNotificationProvider>>().LogTrace("单次预警显示时长：{}", ts);
            ShowNotification(new NotificationRequest()
            {
                MaskContent = new NotificationContent(new WeatherNotificationProviderControl(true, i, ts))
                {
                    SpeechContent = i.Title,
                    Duration = TimeSpan.FromSeconds(5)
                },
                OverlayContent = new NotificationContent(new WeatherNotificationProviderControl(false, i, ts))
                {
                    Duration = ts * 2,
                    SpeechContent = i.Detail,

                }
            });
            ShownAlerts.Add(i.Detail);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}
#endif