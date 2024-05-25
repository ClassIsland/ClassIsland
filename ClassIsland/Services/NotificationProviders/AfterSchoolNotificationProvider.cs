using System;
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

public class AfterSchoolNotificationProvider : INotificationProvider, IHostedService
{
    public NotificationHostService NotificationHostService { get; }
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

    public AfterSchoolNotificationProvider(NotificationHostService notificationHostService, AttachedSettingsHostService attachedSettingsHostService)
    {
        NotificationHostService = notificationHostService;
        NotificationHostService.RegisterNotificationProvider(this);
        Settings =
            NotificationHostService.GetNotificationProviderSettings<AfterSchoolNotificationProviderSettings>(ProviderGuid) ??
            new AfterSchoolNotificationProviderSettings();
        NotificationHostService.WriteNotificationProviderSettings(ProviderGuid, Settings);
        NotificationHostService.CurrentStateChanged += NotificationHostServiceOnCurrentStateChanged;
        SettingsElement = new AfterSchoolNotificationProviderSettingsControl(Settings);

        var item = typeof(AfterSchoolNotificationAttachedSettingsControl);
        attachedSettingsHostService.ClassPlanSettingsAttachedSettingsControls.Add(item);
        attachedSettingsHostService.TimeLayoutSettingsAttachedSettingsControls.Add(item);
    }

    private void NotificationHostServiceOnCurrentStateChanged(object? sender, EventArgs e)
    {
        var settings = (IAfterSchoolNotificationProviderSettingsBase?)GetAttachedSettings() ?? Settings;
        if (!settings.IsEnabled || NotificationHostService.CurrentState != TimeState.None || !NotificationHostService.IsClassPlanLoaded)
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
        var mvm = App.GetService<MainWindow>().ViewModel;
        var settings = AttachedSettingsHostService
            .GetAttachedSettingsByPriority<
                AfterSchoolNotificationAttachedSettings>(
                ProviderGuid,
                classPlan: mvm.CurrentClassPlan,
                timeLayout: mvm.CurrentClassPlan?.TimeLayout
            );
        return settings;
    }
}