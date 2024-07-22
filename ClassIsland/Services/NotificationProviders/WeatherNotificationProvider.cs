using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ClassIsland.Controls.AttachedSettingsControls;
using ClassIsland.Controls.NotificationProviders;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared.Abstraction.Models;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Notification;
using ClassIsland.Models.AttachedSettings;
using ClassIsland.Models.NotificationProviderSettings;
using ClassIsland.Shared;
using MaterialDesignThemes.Wpf;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.NotificationProviders;

public class WeatherNotificationProvider : INotificationProvider, IHostedService
{
    public string Name { get; set; } = "天气预警";
    public string Description { get; set; } = "当有降雨或者极端天气时发出提醒。";
    public Guid ProviderGuid { get; set; } = new Guid("7625DE96-38AA-4B71-B478-3F156DD9458D");
    public object? SettingsElement { get; set; }
    public object? IconElement { get; set; } = new PackIcon()
    {
        Kind = PackIconKind.CloudWarning,
        Width = 24,
        Height = 24
    };

    private IWeatherService WeatherService { get; }

    private SettingsService SettingsService { get; }

    private WeatherNotificationProviderSettings Settings { get; }

    private INotificationHostService NotificationHostService { get; }

    private IAttachedSettingsHostService AttachedSettingsHostService { get; }

    private ILessonsService LessonsService { get; }

    private List<string> ShownAlerts { get; } = new();

    public WeatherNotificationProvider(INotificationHostService notificationHostService,
        IAttachedSettingsHostService attachedSettingsHostService,
        IWeatherService weatherService,
        SettingsService settingsService,
        ILessonsService lessonsService)
    {
        NotificationHostService = notificationHostService;
        WeatherService = weatherService;
        SettingsService = settingsService;
        AttachedSettingsHostService = attachedSettingsHostService;
        LessonsService = lessonsService;

        NotificationHostService.RegisterNotificationProvider(this);
        attachedSettingsHostService.TimePointSettingsAttachedSettingsControls.Add(typeof(WeatherNotificationAttachedSettingsControl));
        //attachedSettingsHostService.SubjectSettingsAttachedSettingsControls.Add(typeof(WeatherNotificationAttachedSettingsControl));

        Settings = NotificationHostService.GetNotificationProviderSettings
                       <WeatherNotificationProviderSettings>(ProviderGuid)
                   ?? new WeatherNotificationProviderSettings();
        NotificationHostService.WriteNotificationProviderSettings(ProviderGuid, Settings);
        SettingsElement = new WeatherNotificationProviderSettingsControl(Settings);

        LessonsService.OnBreakingTime += NotificationHostServiceOnOnBreakingTime;
        LessonsService.OnClass += NotificationHostServiceOnOnClass;
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
        NotificationHostService.ShowNotification(new NotificationRequest()
        {
            MaskContent = new WeatherForecastNotificationProvider(true, SettingsService.Settings.LastWeatherInfo),
            OverlayContent = new WeatherForecastNotificationProvider(false, SettingsService.Settings.LastWeatherInfo),
            OverlayDuration = TimeSpan.FromSeconds(15)
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

        foreach (var i in SettingsService.Settings.LastWeatherInfo.Alerts.Where(i => !ShownAlerts.Contains(i.Detail)))
        {
            var t = i.Detail.Length / Settings.WeatherAlertSpeed;
            if (t <= 10) t = 10.0;
            if (t >= 90) t = 90.0;
            var ts = TimeSpan.FromSeconds(t);
            IAppHost.GetService<ILogger<WeatherNotificationProvider>>().LogTrace("单次预警显示时长：{}", ts);
            NotificationHostService.ShowNotification(new NotificationRequest()
            {
                MaskContent = new WeatherNotificationProviderControl(true, i, ts),
                MaskSpeechContent = i.Title,
                OverlayContent = new WeatherNotificationProviderControl(false, i, ts),
                OverlaySpeechContent = i.Detail,
                OverlayDuration = ts * 2,
                MaskDuration = TimeSpan.FromSeconds(5)
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