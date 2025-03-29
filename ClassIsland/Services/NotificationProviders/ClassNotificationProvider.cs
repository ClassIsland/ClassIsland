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
using ClassIsland.Models;
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

    private NotificationRequest? _onClassNotificationRequest;
    
    private INotificationHostService NotificationHostService { get; }

    private ILessonsService LessonsService { get; }

    private IExactTimeService ExactTimeService { get; }

    public ClassNotificationProvider(INotificationHostService notificationHostService, IAttachedSettingsHostService attachedSettingsHostService , ILessonsService lessonsService, IExactTimeService exactTimeService)
    {
        NotificationHostService = notificationHostService;
        LessonsService = lessonsService;
        ExactTimeService = exactTimeService;

        NotificationHostService.RegisterNotificationProvider(this);
        LessonsService.OnClass += OnClass;
        LessonsService.OnBreakingTime += OnBreakingTime;
        LessonsService.PostMainTimerTicked += UpdateTimerTick;
        Settings = NotificationHostService.GetNotificationProviderSettings
                       <ClassNotificationSettings>(ProviderGuid);
        SettingsElement = new ClassNotificationProviderSettingsControl(Settings);

        var item = typeof(ClassNotificationAttachedSettingsControl);
    }

    private void UpdateTimerTick(object? sender, EventArgs e)
    {
        var tClassDelta = LessonsService.OnClassLeftTime;
        var settingsSource = GetEffectiveSettings();

        if (!settingsSource.IsClassOnPreparingNotificationEnabled ||
            LessonsService.CurrentState is not (TimeState.Breaking or TimeState.None))
            return;

        var settingsDeltaTime = GetSettingsDeltaTime(settingsSource);
        if (tClassDelta > TimeSpan.Zero && tClassDelta <= TimeSpan.FromSeconds(settingsDeltaTime))
        {
            if (IsClassPreparingNotified)
                return;

            IsClassPreparingNotified = true;
            var deltaTime = CalculateDeltaTime(settingsDeltaTime, tClassDelta);
            var notificationRequest = BuildNotificationRequest(settingsSource, deltaTime);
            NotificationHostService.ShowNotification(notificationRequest);
        }
        else if (IsClassPreparingNotified)
        {
            _onClassNotificationRequest?.CancellationTokenSource.Cancel();
            IsClassPreparingNotified = false;
        }
    }

    private IClassNotificationSettings GetEffectiveSettings()
    {
        var settings = GetAttachedSettingsNext();
        var isAttachedSettingsEnabled = settings?.IsAttachSettingsEnabled == true;
        return isAttachedSettingsEnabled ? settings! : Settings;
    }

    private int GetSettingsDeltaTime(IClassNotificationSettings settingsSource)
    {
        var isOutDoor = LessonsService.NextClassSubject.IsOutDoor;
        return isOutDoor ? Settings.OutDoorClassPreparingDeltaTime : Settings.InDoorClassPreparingDeltaTime;
    }

    private TimeSpan CalculateDeltaTime(int settingsDeltaTime, TimeSpan tClassDelta)
    {
        var deltaTime = TimeSpan.FromSeconds(settingsDeltaTime) - tClassDelta;
        return deltaTime > TimeSpan.FromSeconds(10) ? tClassDelta : TimeSpan.FromSeconds(settingsDeltaTime);
    }

    private NotificationRequest BuildNotificationRequest(IClassNotificationSettings settingsSource, TimeSpan deltaTime)
    {
        var message = GetNotificationMessage(settingsSource);

        return _onClassNotificationRequest = new NotificationRequest
        {
            MaskSpeechContent = $"距上课还剩{TimeSpanFormatHelper.Format(deltaTime)}。",
            MaskContent = new ClassNotificationProviderControl("ClassPrepareNotifyMask")
            {
                MaskMessage = settingsSource.ClassOnPreparingMaskText
            },
            MaskDuration = TimeSpan.FromSeconds(3),
            OverlaySpeechContent = $"{message} 下节课是：{LessonsService.NextClassSubject.Name} {(Settings.ShowTeacherName ? FormatTeacher(LessonsService.NextClassSubject) : "")}。",
            OverlayContent = new ClassNotificationProviderControl("ClassPrepareNotifyOverlay")
            {
                Message = message,
                ShowTeacherName = Settings.ShowTeacherName
            },
            TargetOverlayEndTime = DateTimeToCurrentDateTimeConverter.Convert(LessonsService.NextClassTimeLayoutItem.StartSecond),
            IsSpeechEnabled = Settings.IsSpeechEnabledOnClassPreparing
        };
    }

    private string GetNotificationMessage(IClassNotificationSettings settingsSource)
    {
        if (settingsSource is null)
            return string.Empty;

        if (LessonsService.NextClassSubject.IsOutDoor)
            return settingsSource.OutdoorClassOnPreparingText;

        return settingsSource.ClassOnPreparingText;
    }


    private void OnBreakingTime(object? sender, EventArgs e)
    {
        IsClassPreparingNotified = false;
        var settings = GetAttachedSettings();
        var settingsIsClassOffNotificationEnabled = settings?.IsAttachSettingsEnabled == true ?
            settings.IsClassOffNotificationEnabled
            : Settings.IsClassOffNotificationEnabled;


        if (!settingsIsClassOffNotificationEnabled ||
            LessonsService.CurrentTimeLayoutItem == TimeLayoutItem.Empty ||
            ExactTimeService.GetCurrentLocalDateTime().TimeOfDay - LessonsService.CurrentTimeLayoutItem.StartSecond.TimeOfDay > TimeSpan.FromSeconds(5))
            return;
        var overlayText = settings?.ClassOffOverlayText ?? Settings.ClassOffOverlayText;
        var showOverlayText = !string.IsNullOrWhiteSpace(overlayText);

        if (LessonsService.NextClassSubject != Subject.Empty)
        {
            NotificationHostService.ShowNotification(new NotificationRequest()
            {
                MaskContent = new ClassNotificationProviderControl("ClassOffNotification")
                {
                    ShowTeacherName = Settings.ShowTeacherName
                },
                MaskDuration = TimeSpan.FromSeconds(2),
                MaskSpeechContent = LessonsService.CurrentTimeLayoutItem.BreakNameText,
                OverlayContent = new ClassNotificationProviderControl("ClassOffOverlay")
                {
                    ShowTeacherName = Settings.ShowTeacherName,
                    Message = overlayText
                },
                OverlayDuration = showOverlayText ? TimeSpan.FromSeconds(20) : TimeSpan.FromSeconds(10),
                OverlaySpeechContent = $"本节{LessonsService.CurrentTimeLayoutItem.BreakNameText}常{TimeSpanFormatHelper.Format(LessonsService.CurrentTimeLayoutItem.Last)}，下节课是：{LessonsService.NextClassSubject.Name} {(Settings.ShowTeacherName ? FormatTeacher(LessonsService.NextClassSubject) : "")}。{overlayText}",
                IsSpeechEnabled = Settings.IsSpeechEnabledOnClassOff
            });
        }
        else
        {
            NotificationHostService.ShowNotification(new NotificationRequest()
            {
                MaskContent = new ClassNotificationProviderControl("ClassOffNotification")
                {
                    ShowTeacherName = Settings.ShowTeacherName
                },
                MaskDuration = TimeSpan.FromSeconds(5),
                MaskSpeechContent = LessonsService.CurrentTimeLayoutItem.BreakNameText,
                IsSpeechEnabled = Settings.IsSpeechEnabledOnClassOff
            });
        }
    }

    private void OnClass(object? sender, EventArgs e)
    {
        IsClassPreparingNotified = false;
        var settings = GetAttachedSettings();
        var settingsIsClassOnNotificationEnabled = settings?.IsAttachSettingsEnabled == true ? 
            settings.IsClassOnNotificationEnabled 
            : Settings.IsClassOnNotificationEnabled;
        var settingsSource = (IClassNotificationSettings?)(settings?.IsAttachSettingsEnabled == true ? settings : Settings) ?? Settings;

        if (!settingsIsClassOnNotificationEnabled ||
            IsClassOnNotified ||
            LessonsService.CurrentTimeLayoutItem == TimeLayoutItem.Empty ||
            ExactTimeService.GetCurrentLocalDateTime().TimeOfDay - LessonsService.CurrentTimeLayoutItem.StartSecond.TimeOfDay > TimeSpan.FromSeconds(5))
            return;

        if (IsClassPreparingNotified)
        {
            IsClassPreparingNotified = false;
        }
        NotificationHostService.ShowNotification(new NotificationRequest()
        {
            MaskContent = new ClassNotificationProviderControl("ClassOnNotification")
            {
                MaskMessage = settingsSource.ClassOnMaskText
            },
            MaskSpeechContent = settingsSource.ClassOnMaskText,
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
