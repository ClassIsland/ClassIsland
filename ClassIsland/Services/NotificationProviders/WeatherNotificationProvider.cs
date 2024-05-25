using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ClassIsland.Controls.AttachedSettingsControls;
using ClassIsland.Controls.NotificationProviders;
using ClassIsland.Core.Abstraction.Models;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Interfaces;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Models.AttachedSettings;
using ClassIsland.Models.NotificationProviderSettings;

using MaterialDesignThemes.Wpf;

using Microsoft.Extensions.Hosting;

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

    private WeatherService WeatherService { get; }

    private SettingsService SettingsService { get; }

    private WeatherNotificationProviderSettings Settings { get; }

    private NotificationHostService NotificationHostService { get; }

    private AttachedSettingsHostService AttachedSettingsHostService { get; }

    private List<string> ShownAlerts { get; } = new();

    public WeatherNotificationProvider(NotificationHostService notificationHostService,
        AttachedSettingsHostService attachedSettingsHostService,
        WeatherService weatherService,
        SettingsService settingsService)
    {
        NotificationHostService = notificationHostService;
        WeatherService = weatherService;
        SettingsService = settingsService;
        AttachedSettingsHostService = attachedSettingsHostService;
        NotificationHostService.RegisterNotificationProvider(this);
        attachedSettingsHostService.TimePointSettingsAttachedSettingsControls.Add(typeof(WeatherNotificationAttachedSettingsControl));
        //attachedSettingsHostService.SubjectSettingsAttachedSettingsControls.Add(typeof(WeatherNotificationAttachedSettingsControl));

        Settings = NotificationHostService.GetNotificationProviderSettings
                       <WeatherNotificationProviderSettings>(ProviderGuid)
                   ?? new WeatherNotificationProviderSettings();
        NotificationHostService.WriteNotificationProviderSettings(ProviderGuid, Settings);
        SettingsElement = new WeatherNotificationProviderSettingsControl(Settings);

        NotificationHostService.OnBreakingTime += NotificationHostServiceOnOnBreakingTime;
        NotificationHostService.OnClass += NotificationHostServiceOnOnClass;
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
        var s = (IWeatherNotificationSettingsBase?)AttachedSettingsHostService
            .GetAttachedSettingsByPriority<WeatherNotificationAttachedSettings>(ProviderGuid,
                timeLayoutItem: App.GetService<MainWindow>().ViewModel.CurrentTimeLayoutItem) ?? Settings;
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
        var s = (IWeatherNotificationSettingsBase?)AttachedSettingsHostService
            .GetAttachedSettingsByPriority<WeatherNotificationAttachedSettings>(ProviderGuid,
                timeLayoutItem: App.GetService<MainWindow>().ViewModel.CurrentTimeLayoutItem) ?? Settings;
        if (!Settings.IsAlertEnabled || s.AlertShowMode == NotificationModes.Disabled ||
            (s.AlertShowMode == NotificationModes.Default &&
             Settings.AlertShowMode == NotificationModes.Disabled))
        {
            return;
        }

        foreach (var i in SettingsService.Settings.LastWeatherInfo.Alerts.Where(i => !ShownAlerts.Contains(i.Detail)))
        {
            NotificationHostService.ShowNotification(new NotificationRequest()
            {
                MaskContent = new WeatherNotificationProviderControl(true, i),
                MaskSpeechContent = i.Title,
                OverlayContent = new WeatherNotificationProviderControl(false, i),
                OverlaySpeechContent = i.Detail,
                OverlayDuration = TimeSpan.FromSeconds(40),
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