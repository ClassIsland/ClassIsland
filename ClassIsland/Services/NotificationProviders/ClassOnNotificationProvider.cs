using System;
using System.Threading;
using System.Threading.Tasks;

using ClassIsland.Controls.AttachedSettingsControls;
using ClassIsland.Controls.NotificationProviders;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Notification;
using ClassIsland.Helpers;
using ClassIsland.Models;
using ClassIsland.Models.AttachedSettings;
using ClassIsland.Models.NotificationProviderSettings;
using ClassIsland.Shared.Models.Profile;
using MaterialDesignThemes.Wpf;

using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services.NotificationProviders;

public class ClassOnNotificationProvider : INotificationProvider, IHostedService
{
    public string Name { get; set; } = "上课提醒";
    public string Description { get; set; } = "在上课时发出醒目提醒。";
    public Guid ProviderGuid { get; set; } = new Guid("C308812E-3C3A-6E75-99A1-E6FC0D41B04A");
    public object? SettingsElement { get; set; }
    public object? IconElement { get; set; } = new PackIcon()
    {
        Kind = PackIconKind.Notifications,
        Width = 24,
        Height = 24
    };

    private ClassOnNotificationSettings Settings
    {
        get;
        set;
    } = new();

    private bool IsClassOnNotified { get; set; } = false;
    
    private INotificationHostService NotificationHostService { get; }

    private ILessonsService LessonsService { get; }

    private IExactTimeService ExactTimeService { get; }

    public ClassOnNotificationProvider(INotificationHostService notificationHostService, IAttachedSettingsHostService attachedSettingsHostService , ILessonsService lessonsService, IExactTimeService exactTimeService)
    {
        NotificationHostService = notificationHostService;
        LessonsService = lessonsService;
        ExactTimeService = exactTimeService;

        NotificationHostService.RegisterNotificationProvider(this);
        LessonsService.OnClass += OnClass;
        Settings = NotificationHostService.GetNotificationProviderSettings
                       <ClassOnNotificationSettings>(ProviderGuid);
        SettingsElement = new ClassOnNotificationProviderSettingsControl(Settings);

        var item = typeof(ClassOnNotificationAttachedSettingsControl);
    }

    private void OnClass(object? sender, EventArgs e)
    {
        var settings = GetAttachedSettings();
        var settingsIsClassOnNotificationEnabled = settings?.IsAttachSettingsEnabled == true ? 
            settings.IsClassOnNotificationEnabled 
            : Settings.IsClassOnNotificationEnabled;
        var settingsSource = (IClassOnNotificationSettings?)(settings?.IsAttachSettingsEnabled == true ? settings : Settings) ?? Settings;

        if (!settingsIsClassOnNotificationEnabled ||
            IsClassOnNotified ||
            LessonsService.CurrentTimeLayoutItem == TimeLayoutItem.Empty ||
            ExactTimeService.GetCurrentLocalDateTime().TimeOfDay - LessonsService.CurrentTimeLayoutItem.StartSecond.TimeOfDay > TimeSpan.FromSeconds(5))
            return;

        NotificationHostService.ShowNotification(new NotificationRequest()
        {
            MaskContent = new ClassOnNotificationProviderControl("ClassOnNotification")
            {
                MaskMessage = settingsSource.ClassOnMaskText
            },
            MaskSpeechContent = settingsSource.ClassOnMaskText,
            IsSpeechEnabled = Settings.IsSpeechEnabledOnClassOn
        });
    }

    private ClassOnNotificationAttachedSettings? GetAttachedSettings()
    {
        var mvm = App.GetService<MainWindow>().ViewModel;
        var settings = IAttachedSettingsHostService
            .GetAttachedSettingsByPriority<
                ClassOnNotificationAttachedSettings>(
                ProviderGuid,
                LessonsService.CurrentSubject,
                LessonsService.CurrentTimeLayoutItem,
                LessonsService.CurrentClassPlan,
                LessonsService.CurrentClassPlan?.TimeLayout
            );
        return settings;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return new Task(() => {});
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return new Task(() => { });
    }
}
