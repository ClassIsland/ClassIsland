using System;
using System.Threading;
using System.Threading.Tasks;

using ClassIsland.Controls.AttachedSettingsControls;
using ClassIsland.Controls.NotificationProviders;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Helpers;
using ClassIsland.Models;
using ClassIsland.Models.AttachedSettings;
using ClassIsland.Models.NotificationProviderSettings;
using ClassIsland.Shared.Models.Profile;
using MaterialDesignThemes.Wpf;

using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services.NotificationProviders;

[NotificationProviderInfo("08F0D9C3-C770-4093-A3D0-02F3D90C24BC", "上课提醒", PackIconKind.Notifications, "在准备上课、上课和下课时发出醒目提醒，并预告下一节课程。")]
public class ClassNotificationProvider : NotificationProviderBase<ClassNotificationSettings>
{
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

        LessonsService.OnClass += OnClass;
        LessonsService.OnBreakingTime += OnBreakingTime;
        LessonsService.PostMainTimerTicked += UpdateTimerTick;
    }

    private void UpdateTimerTick(object? sender, EventArgs e)
    {
        var tClassDelta = LessonsService.OnClassLeftTime;
        var settingsSource = GetEffectiveSettings();

        if (!settingsSource.IsClassOnPreparingNotificationEnabled ||
            LessonsService.CurrentState is not (TimeState.Breaking or TimeState.None))
            return;

        var settingsDeltaTime = GetSettingsDeltaTime();
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


    private IClassNotificationSettings GetEffectiveSettings()
    {
        var settings = GetAttachedSettingsNext();
        var isAttachedSettingsEnabled = settings?.IsAttachSettingsEnabled == true;
        return isAttachedSettingsEnabled ? settings! : Settings;
    }

    private int GetSettingsDeltaTime()
    {
        var settings = GetAttachedSettingsNext();
        var isOutDoor = LessonsService.NextClassSubject.IsOutDoor;
        var isAttachedSettingsEnabled = settings?.IsAttachSettingsEnabled == true;
        if (isAttachedSettingsEnabled && settings?.ClassPreparingDeltaTime != null)
        {
            return settings.ClassPreparingDeltaTime;
        }
        else
        {
            return isOutDoor ? Settings.OutDoorClassPreparingDeltaTime : Settings.InDoorClassPreparingDeltaTime;
        }
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
            MaskContent = NotificationContent.CreateTwoIconsMask(settingsSource.ClassOnPreparingMaskText, rightIcon: PackIconKind.Class, factory:
                x =>
                {
                    x.SpeechContent = $"距上课还剩{TimeSpanFormatHelper.Format(deltaTime)}。";
                    x.Duration = TimeSpan.FromSeconds(3);
                    x.IsSpeechEnabled = Settings.IsSpeechEnabledOnClassPreparing;
                }),
            OverlayContent = new NotificationContent(new ClassNotificationProviderControl("ClassPrepareNotifyOverlay")
            {
                Message = message,
                ShowTeacherName = Settings.ShowTeacherName
            })
            {
                SpeechContent = $"{message} 下节课是：{LessonsService.NextClassSubject.Name} {(Settings.ShowTeacherName ? FormatTeacher(LessonsService.NextClassSubject) : "")}。",
                EndTime = DateTimeToCurrentDateTimeConverter.Convert(LessonsService.NextClassTimeLayoutItem.StartSecond),
                IsSpeechEnabled = Settings.IsSpeechEnabledOnClassPreparing
            },
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

        var isNextClassEmpty = LessonsService.NextClassSubject == Subject.Empty;
        ShowNotification(new NotificationRequest()
        {
            MaskContent = new NotificationContent(new ClassNotificationProviderControl("ClassOffNotification")
            {
                ShowTeacherName = Settings.ShowTeacherName
            })
            {
                Duration = isNextClassEmpty? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(2),
                SpeechContent = LessonsService.CurrentTimeLayoutItem.BreakNameText,
                IsSpeechEnabled = Settings.IsSpeechEnabledOnClassOff
            },
            OverlayContent = isNextClassEmpty ? null : new NotificationContent(new ClassNotificationProviderControl("ClassOffOverlay")
            {
                ShowTeacherName = Settings.ShowTeacherName,
                Message = overlayText
            })
            {
                Duration = showOverlayText ? TimeSpan.FromSeconds(20) : TimeSpan.FromSeconds(10),
                SpeechContent = $"本节{LessonsService.CurrentTimeLayoutItem.BreakNameText}常{TimeSpanFormatHelper.Format(LessonsService.CurrentTimeLayoutItem.Last)}，下节课是：{LessonsService.NextClassSubject.Name} {(Settings.ShowTeacherName ? FormatTeacher(LessonsService.NextClassSubject) : "")}。{overlayText}",
                IsSpeechEnabled = Settings.IsSpeechEnabledOnClassOff
            }
        });
        
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
        ShowNotification(new NotificationRequest()
        {
            MaskContent = NotificationContent.CreateTwoIconsMask(settingsSource.ClassOnMaskText, rightIcon: PackIconKind.Class, factory:
                x =>
                {
                    x.IsSpeechEnabled = Settings.IsSpeechEnabledOnClassOn;
                }),
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
