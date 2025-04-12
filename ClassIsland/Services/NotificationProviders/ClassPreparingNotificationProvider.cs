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

public class ClassPreparingNotificationProvider : INotificationProvider, IHostedService
{
    public string Name { get; set; } = "准备上课提醒";
    public string Description { get; set; } = "在准备上课时发出醒目提醒，并预告下一节课程。";
    public Guid ProviderGuid { get; set; } = new Guid("08F0D9C3-C770-4093-A3D0-02F3D90C24BC");
    public object? SettingsElement { get; set; }
    public object? IconElement { get; set; } = new PackIcon()
    {
        Kind = PackIconKind.Notifications,
        Width = 24,
        Height = 24
    };

    private ClassPreparingNotificationSettings Settings
    {
        get;
        set;
    } = new();

    private bool IsClassPreparingNotified { get; set; } = false;

    private string FormatTeacher(Subject subject)
    {
        var name = subject.GetFirstName();
        return string.IsNullOrWhiteSpace(name) ? string.Empty : $"由{name}老师任教";
    }

    private NotificationRequest? _onClassNotificationRequest;
    
    private INotificationHostService NotificationHostService { get; }

    private ILessonsService LessonsService { get; }

    private IExactTimeService ExactTimeService { get; }

    public ClassPreparingNotificationProvider(INotificationHostService notificationHostService, IAttachedSettingsHostService attachedSettingsHostService , ILessonsService lessonsService, IExactTimeService exactTimeService)
    {
        NotificationHostService = notificationHostService;
        LessonsService = lessonsService;
        ExactTimeService = exactTimeService;

        NotificationHostService.RegisterNotificationProvider(this);
        LessonsService.OnClass += OnClass;
        LessonsService.OnBreakingTime += OnBreakingTime;
        LessonsService.PostMainTimerTicked += UpdateTimerTick;
        Settings = NotificationHostService.GetNotificationProviderSettings
                       <ClassPreparingNotificationSettings>(ProviderGuid);
        SettingsElement = new ClassPreparingNotificationProviderSettingsControl(Settings);

        var item = typeof(ClassPreparingNotificationAttachedSettingsControl);
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
            AppBase.Current.Dispatcher.InvokeAsync(() => ShowNotification(notificationRequest));

        }
        else if (IsClassPreparingNotified)
        {
            _onClassNotificationRequest?.CancellationTokenSource.Cancel();
            IsClassPreparingNotified = false;
        }
    }

    private void ShowNotification(NotificationRequest notificationRequest)
    {
        NotificationHostService.ShowNotification(notificationRequest);
    }

    private IClassPreparingNotificationSettings GetEffectiveSettings()
    {
        var settings = GetAttachedSettingsNext();
        var isAttachedSettingsEnabled = settings?.IsAttachSettingsEnabled == true;
        return isAttachedSettingsEnabled ? settings! : Settings;
    }

    private int GetSettingsDeltaTime(IClassPreparingNotificationSettings settingsSource)
    {
        var isOutDoor = LessonsService.NextClassSubject.IsOutDoor;
        return isOutDoor ? Settings.OutDoorClassPreparingDeltaTime : Settings.InDoorClassPreparingDeltaTime;
    }

    private TimeSpan CalculateDeltaTime(int settingsDeltaTime, TimeSpan tClassDelta)
    {
        var deltaTime = TimeSpan.FromSeconds(settingsDeltaTime) - tClassDelta;
        return deltaTime > TimeSpan.FromSeconds(10) ? tClassDelta : TimeSpan.FromSeconds(settingsDeltaTime);
    }

    private NotificationRequest BuildNotificationRequest(IClassPreparingNotificationSettings settingsSource, TimeSpan deltaTime)
    {
        var message = GetNotificationMessage(settingsSource);

        return _onClassNotificationRequest = new NotificationRequest
        {
            MaskSpeechContent = $"距上课还剩{TimeSpanFormatHelper.Format(deltaTime)}。",
            MaskContent = new ClassPreparingNotificationProviderControl("ClassPrepareNotifyMask")
            {
                MaskMessage = settingsSource.ClassOnPreparingMaskText
            },
            MaskDuration = TimeSpan.FromSeconds(3),
            OverlaySpeechContent = $"{message} 下节课是：{LessonsService.NextClassSubject.Name} {(Settings.ShowTeacherNameWhenClassPreparing ? FormatTeacher(LessonsService.NextClassSubject) : "")}。",
            OverlayContent = new ClassPreparingNotificationProviderControl("ClassPrepareNotifyOverlay")
            {
                Message = message,
                ShowTeacherNameWhenClassPreparing = Settings.ShowTeacherNameWhenClassPreparing
            },
            TargetOverlayEndTime = DateTimeToCurrentDateTimeConverter.Convert(LessonsService.NextClassTimeLayoutItem.StartSecond),
            IsSpeechEnabled = Settings.IsSpeechEnabledOnClassPreparing
        };
    }

    private string GetNotificationMessage(IClassPreparingNotificationSettings settingsSource)
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
    }

    private void OnClass(object? sender, EventArgs e)
    {
        IsClassPreparingNotified = false;
    }

    private ClassPreparingNotificationAttachedSettings? GetAttachedSettingsNext()
    {
        var mvm = App.GetService<MainWindow>().ViewModel;
        var settings = IAttachedSettingsHostService
            .GetAttachedSettingsByPriority<
                ClassPreparingNotificationAttachedSettings>(
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
