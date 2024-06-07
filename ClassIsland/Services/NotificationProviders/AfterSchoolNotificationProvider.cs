using System;
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

using MaterialDesignThemes.Wpf;

using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services.NotificationProviders;

public class AfterSchoolNotificationProvider : INotificationProvider, IHostedService
{
    public INotificationHostService NotificationHostService { get; }
    public string Name { get; set; } = "放学提醒";
    public string Description { get; set; } = "在当天的课程结束后发出提醒。";
    public Guid ProviderGuid { get; set; } = new Guid("8FBC3A26-6D20-44DD-B895-B9411E3DDC51");
    public object? SettingsElement { get; set; }
    public object? IconElement { get; set; } = new PackIcon()
    {
        Kind = PackIconKind.HumanRunFast,
        Width = 24,
        Height = 24
    };
    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }

    private AfterSchoolNotificationProviderSettings Settings
    {
        get;
    }

    private ILessonsService LessonsService { get; }

    public AfterSchoolNotificationProvider(INotificationHostService notificationHostService, IAttachedSettingsHostService attachedSettingsHostService, ILessonsService lessonsService)
    {
        NotificationHostService = notificationHostService;
        LessonsService = lessonsService;
        NotificationHostService.RegisterNotificationProvider(this);
        Settings =
            NotificationHostService.GetNotificationProviderSettings<AfterSchoolNotificationProviderSettings>(ProviderGuid) ??
            new AfterSchoolNotificationProviderSettings();
        NotificationHostService.WriteNotificationProviderSettings(ProviderGuid, Settings);
        LessonsService.CurrentTimeStateChanged += NotificationHostServiceOnCurrentStateChanged;
        SettingsElement = new AfterSchoolNotificationProviderSettingsControl(Settings);

        var item = typeof(AfterSchoolNotificationAttachedSettingsControl);
        attachedSettingsHostService.ClassPlanSettingsAttachedSettingsControls.Add(item);
        attachedSettingsHostService.TimeLayoutSettingsAttachedSettingsControls.Add(item);
    }

    private void NotificationHostServiceOnCurrentStateChanged(object? sender, EventArgs e)
    {
        var settings = (IAfterSchoolNotificationProviderSettingsBase?)GetAttachedSettings() ?? Settings;
        if (!settings.IsEnabled || LessonsService.CurrentState != TimeState.None || !LessonsService.IsClassPlanLoaded)
        {
            return;
        }

        NotificationHostService.ShowNotification(new NotificationRequest()
        {
            MaskContent = new AfterSchoolNotificationProviderControl(settings.NotificationMsg, "AfterSchoolMask"),
            MaskSpeechContent = "放学",
            OverlayContent = new AfterSchoolNotificationProviderControl(settings.NotificationMsg, "AfterSchoolOverlay"),
            OverlaySpeechContent = settings.NotificationMsg,
            OverlayDuration = TimeSpan.FromSeconds(30)
        });
    }

    private AfterSchoolNotificationAttachedSettings? GetAttachedSettings()
    {
        var settings = IAttachedSettingsHostService
            .GetAttachedSettingsByPriority<
                AfterSchoolNotificationAttachedSettings>(
                ProviderGuid,
                classPlan: LessonsService.CurrentClassPlan,
                timeLayout: LessonsService.CurrentClassPlan?.TimeLayout
            );
        return settings;
    }
}