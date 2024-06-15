using System;
using System.Threading;
using System.Threading.Tasks;

using ClassIsland.Controls.AttachedSettingsControls;
using ClassIsland.Controls.NotificationProviders;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Notification;
using ClassIsland.Helpers;
using ClassIsland.Models.AttachedSettings;
using ClassIsland.Models.NotificationProviderSettings;
using ClassIsland.Shared.Models.Profile;
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

    private string FormatTeacher(Subject subject)
    {
        var name = subject.GetFirstName();
        return string.IsNullOrWhiteSpace(name) ? string.Empty : $"由{name}老师任教";
    }
    
    private INotificationHostService NotificationHostService { get; }

    private ILessonsService LessonsService { get; }

    public ClassNotificationProvider(INotificationHostService notificationHostService, IAttachedSettingsHostService attachedSettingsHostService , ILessonsService lessonsService)
    {
        NotificationHostService = notificationHostService;
        LessonsService = lessonsService;

        NotificationHostService.RegisterNotificationProvider(this);
        LessonsService.OnClass += OnClass;
        LessonsService.OnBreakingTime += OnBreakingTime;
        LessonsService.PostMainTimerTicked += UpdateTimerTick;
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
        var tClassDelta = LessonsService.OnClassLeftTime;
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
        var settingsDeltaTime = LessonsService.NextClassSubject.IsOutDoor
            ? settingsOutDoorClassPreparingDeltaTime
            : settingsInDoorClassPreparingDeltaTime;
        if (settingsIsClassOnPreparingNotificationEnabled &&
            tClassDelta > TimeSpan.Zero &&
              tClassDelta <= TimeSpan.FromSeconds(settingsDeltaTime) &&
            !IsClassPreparingNotified && LessonsService.CurrentState == TimeState.Breaking)
        {
            var deltaTime = LessonsService.NextClassSubject.IsOutDoor
                ? settingsOutDoorClassPreparingDeltaTime
                : settingsInDoorClassPreparingDeltaTime;
            IsClassPreparingNotified = true;
            IsClassOnNotified = true;
            NotificationHostService.ShowNotification(new NotificationRequest()
            {
                MaskSpeechContent = $"距上课还剩{TimeSpanFormatHelper.Format(TimeSpan.FromSeconds(deltaTime))}。",
                MaskContent = new ClassNotificationProviderControl("ClassPrepareNotifyMask"),
                MaskDuration = TimeSpan.FromSeconds(5),
                OverlaySpeechContent = $"{message} 下节课是：{LessonsService.NextClassSubject.Name} {(Settings.ShowTeacherName ? FormatTeacher(LessonsService.NextClassSubject) : "")}。",
                OverlayContent = new ClassNotificationProviderControl("ClassPrepareNotifyOverlay")
                {
                    Message = message,
                    ShowTeacherName = Settings.ShowTeacherName
                },
                TargetOverlayEndTime = DateTimeToCurrentDateTimeConverter.Convert(LessonsService.NextClassTimeLayoutItem.StartSecond),
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
            MaskContent = new ClassNotificationProviderControl("ClassOffNotification")
            {
                ShowTeacherName = Settings.ShowTeacherName
            },
            MaskDuration = TimeSpan.FromSeconds(2),
            MaskSpeechContent = "课间休息",
            OverlayContent = new ClassNotificationProviderControl("ClassOffOverlay")
            {
                ShowTeacherName = Settings.ShowTeacherName
            },
            OverlayDuration = TimeSpan.FromSeconds(10),
            OverlaySpeechContent = $"本节课间休息长{TimeSpanFormatHelper.Format(LessonsService.CurrentTimeLayoutItem.Last)}，下节课是：{LessonsService.NextClassSubject.Name} {(Settings.ShowTeacherName ? FormatTeacher(LessonsService.NextClassSubject) : "")}。",
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
        var settings = IAttachedSettingsHostService
            .GetAttachedSettingsByPriority<
                ClassNotificationAttachedSettings>(
                ProviderGuid,
                LessonsService.CurrentSubject,
                LessonsService.CurrentTimeLayoutItem,
                LessonsService.CurrentClassPlan,
                LessonsService.CurrentClassPlan?.TimeLayout
            );
        return settings;
    }

    private ClassNotificationAttachedSettings? GetAttachedSettingsNext()
    {
        var mvm = App.GetService<MainWindow>().ViewModel;
        var settings = IAttachedSettingsHostService
            .GetAttachedSettingsByPriority<
                ClassNotificationAttachedSettings>(
                ProviderGuid,
                LessonsService.NextClassSubject,
                LessonsService.NextClassTimeLayoutItem,
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