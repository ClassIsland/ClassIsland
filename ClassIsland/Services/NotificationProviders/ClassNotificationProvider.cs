using System;
using System.Threading;
using System.Threading.Tasks;

using ClassIsland.Controls.AttachedSettingsControls;
using ClassIsland.Controls.NotificationProviders;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Interfaces;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Helpers;
using ClassIsland.Models.AttachedSettings;
using ClassIsland.Models.NotificationProviderSettings;

using MaterialDesignThemes.Wpf;

using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services.NotificationProviders;

public class ClassNotificationProvider : INotificationProvider, IHostedService
{
    public string Name { get; set; } = "上课提醒";
    public string Description { get; set; } = "在准备上课、上课和下课时发出醒目提醒，并预告下一节课程。";
    public Guid ProviderGuid { get; set; } = new Guid("08F0D9C3-C770-4093-A3D0-02F3D90C24BC");
    public object? SettingsElement { get; set; }
    public object? IconElement { get; set; } = new PackIcon()
    {
        Kind = PackIconKind.Notifications,
        Width = 24,
        Height = 24
    };

    private ClassNotificationSettings Settings
    {
        get;
        set;
    } = new();

    private bool IsClassPreparingNotified { get; set; } = false;

    private bool IsClassOnNotified { get; set; } = false;

    private NotificationHostService NotificationHostService { get; }

    public ClassNotificationProvider(NotificationHostService notificationHostService, AttachedSettingsHostService attachedSettingsHostService)
    {
        NotificationHostService = notificationHostService;
        NotificationHostService.RegisterNotificationProvider(this);
        NotificationHostService.OnClass += OnClass;
        NotificationHostService.OnBreakingTime += OnBreakingTime;
        NotificationHostService.UpdateTimerTick += UpdateTimerTick;
        Settings = NotificationHostService.GetNotificationProviderSettings
                       <ClassNotificationSettings>(ProviderGuid)
                   ?? new ClassNotificationSettings();
        SettingsElement = new ClassNotificationProviderSettingsControl(Settings);
        NotificationHostService.WriteNotificationProviderSettings(ProviderGuid, Settings);

        var item = typeof(ClassNotificationAttachedSettingsControl);
        attachedSettingsHostService.ClassPlanSettingsAttachedSettingsControls.Add(item);
        attachedSettingsHostService.SubjectSettingsAttachedSettingsControls.Add(item);
        attachedSettingsHostService.TimeLayoutSettingsAttachedSettingsControls.Add(item);
        attachedSettingsHostService.TimePointSettingsAttachedSettingsControls.Add(item);
    }

    private void UpdateTimerTick(object? sender, EventArgs e)
    {
        var tClassDelta = NotificationHostService.OnClassDeltaTime;
        var settings = GetAttachedSettingsNext();
        var isAttachedSettingsEnabled = settings?.IsAttachSettingsEnabled == true;
        var settingsIsClassOnPreparingNotificationEnabled = isAttachedSettingsEnabled ?
            settings!.IsClassOnPreparingNotificationEnabled
            : Settings.IsClassOnPreparingNotificationEnabled;
        var settingsInDoorClassPreparingDeltaTime = isAttachedSettingsEnabled ? 
                settings!.ClassPreparingDeltaTime
                : Settings.InDoorClassPreparingDeltaTime;
        var settingsOutDoorClassPreparingDeltaTime = isAttachedSettingsEnabled ?
                settings!.ClassPreparingDeltaTime
                : Settings.OutDoorClassPreparingDeltaTime;
        var message = isAttachedSettingsEnabled
            ? settings!.ClassOnPreparingText
            : Settings.ClassOnPreparingText;
        var settingsDeltaTime = NotificationHostService.NextClassSubject.IsOutDoor
            ? settingsOutDoorClassPreparingDeltaTime
            : settingsInDoorClassPreparingDeltaTime;
        if (settingsIsClassOnPreparingNotificationEnabled &&
            tClassDelta > TimeSpan.Zero &&
              tClassDelta <= TimeSpan.FromSeconds(settingsDeltaTime) &&
            !IsClassPreparingNotified && NotificationHostService.CurrentState == TimeState.Breaking)
        {
            var deltaTime = NotificationHostService.NextClassSubject.IsOutDoor
                ? settingsOutDoorClassPreparingDeltaTime
                : settingsInDoorClassPreparingDeltaTime;
            IsClassPreparingNotified = true;
            IsClassOnNotified = true;
            NotificationHostService.ShowNotification(new NotificationRequest()
            {
                MaskSpeechContent = $"距上课还剩{TimeSpanFormatHelper.Format(TimeSpan.FromSeconds(deltaTime))}。",
                MaskContent = new ClassNotificationProviderControl("ClassPrepareNotifyMask"),
                MaskDuration = TimeSpan.FromSeconds(5),
                OverlaySpeechContent = $"{message} 下节课是：{NotificationHostService.NextClassSubject.Name}。",
                OverlayContent = new ClassNotificationProviderControl("ClassPrepareNotifyOverlay")
                {
                    Message = message
                },
                TargetOverlayEndTime = DateTimeToCurrentDateTimeConverter.Convert(NotificationHostService.NextClassTimeLayoutItem.StartSecond),
                IsSpeechEnabled = Settings.IsSpeechEnabledOnClassPreparing
            });

            var onClassRequest = new NotificationRequest()
            {
                MaskSpeechContent = "上课",
                MaskContent = new ClassNotificationProviderControl("ClassOnNotification"),
                IsSpeechEnabled = Settings.IsSpeechEnabledOnClassOn
            };
            onClassRequest.CompletedTokenSource.Token.Register((o, token) =>
            {
                IsClassOnNotified = false;
            }, false);
            NotificationHostService.ShowNotification(onClassRequest);
        }
    }

    private void OnBreakingTime(object? sender, EventArgs e)
    {
        IsClassPreparingNotified = false;
        var settings = GetAttachedSettings();
        var settingsIsClassOffNotificationEnabled = settings?.IsAttachSettingsEnabled == true ?
            settings.IsClassOffNotificationEnabled
            : Settings.IsClassOffNotificationEnabled;
        if (!settingsIsClassOffNotificationEnabled)
        {
            return;
        }
        NotificationHostService.ShowNotification(new NotificationRequest()
        {
            MaskContent = new ClassNotificationProviderControl("ClassOffNotification"),
            MaskDuration = TimeSpan.FromSeconds(2),
            MaskSpeechContent = "课间休息",
            OverlayContent = new ClassNotificationProviderControl("ClassOffOverlay"),
            OverlayDuration = TimeSpan.FromSeconds(10),
            OverlaySpeechContent = $"本节课间休息长{TimeSpanFormatHelper.Format(App.GetService<MainWindow>().ViewModel.CurrentTimeLayoutItem.Last)}，下节课是：{App.GetService<MainWindow>().ViewModel.NextSubject.Name}。",
            IsSpeechEnabled = Settings.IsSpeechEnabledOnClassOff
        });
    }

    private void OnClass(object? sender, EventArgs e)
    {
        var settings = GetAttachedSettings();
        var settingsIsClassOnNotificationEnabled = settings?.IsAttachSettingsEnabled == true ? 
            settings.IsClassOnNotificationEnabled 
            : Settings.IsClassOnNotificationEnabled;
        
        if (!settingsIsClassOnNotificationEnabled)
        {
            return;
        }
        if (IsClassOnNotified)
        {
            return;
        }

        if (IsClassPreparingNotified)
        {
            IsClassPreparingNotified = false;
        }
        NotificationHostService.ShowNotification(new NotificationRequest()
        {
            MaskContent = new ClassNotificationProviderControl("ClassOnNotification"),
            MaskSpeechContent = "上课。",
            IsSpeechEnabled = Settings.IsSpeechEnabledOnClassOn
        });
    }

    private ClassNotificationAttachedSettings? GetAttachedSettings()
    {
        var mvm = App.GetService<MainWindow>().ViewModel;
        var settings = AttachedSettingsHostService
            .GetAttachedSettingsByPriority<
                ClassNotificationAttachedSettings>(
                ProviderGuid,
                mvm.CurrentSubject,
                mvm.CurrentTimeLayoutItem,
                mvm.CurrentClassPlan,
                mvm.CurrentClassPlan?.TimeLayout
            );
        return settings;
    }

    private ClassNotificationAttachedSettings? GetAttachedSettingsNext()
    {
        var mvm = App.GetService<MainWindow>().ViewModel;
        var settings = AttachedSettingsHostService
            .GetAttachedSettingsByPriority<
                ClassNotificationAttachedSettings>(
                ProviderGuid,
                mvm.NextSubject,
                mvm.NextTimeLayoutItem,
                mvm.CurrentClassPlan,
                mvm.CurrentClassPlan?.TimeLayout
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