using System;
using System.Collections.Generic;
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

[NotificationProviderInfo("08F0D9C3-C770-4093-A3D0-02F3D90C24BC", "上下课提醒", PackIconKind.Notifications, "在准备上课、上课和下课时发出醒目提醒，并预告下一节课程。")]
[NotificationChannelInfo(PrepareOnClassChannelId, "准备上课提醒", PackIconKind.Class, description:"在上课前指定时间发出提醒。")]
[NotificationChannelInfo(OnClassChannelId, "上课提醒", PackIconKind.Class, description: "在上课时发出提醒。")]
[NotificationChannelInfo(OnBreakingChannelId, "下课提醒", PackIconKind.ClockOutline, description: "在下课时发出提醒。")]
public class ClassNotificationProvider : NotificationProviderBase<ClassNotificationSettings>
{
    private const string PrepareOnClassChannelId = "CDDFE7FF-B904-4C73-B458-82793B2F66E9";
    private const string OnClassChannelId = "AFF5B9A4-037C-4A71-8563-C9EA87DDA75C";
    private const string OnBreakingChannelId = "77C9F3FB-0A2A-4B22-BDDF-3C333462B2F9";

    private bool IsClassPreparingNotified { get; set; } = false;

    private bool IsClassOnNotified { get; set; } = false;

    private string FormatTeacher(Subject subject)
    {
        var name = subject.GetFirstName();
        return string.IsNullOrWhiteSpace(name) ? string.Empty : $"由{name}老师任教";
    }

    private NotificationRequest? _onClassNotificationRequest;

    private NotificationRequest? _prepareOnClassNotificationRequest;

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

        if (LessonsService.CurrentState is not (TimeState.Breaking or TimeState.None))
        {
            return;
        }

        var settingsDeltaTime = GetSettingsDeltaTime();
        if (tClassDelta > TimeSpan.Zero && tClassDelta <= TimeSpanHelper.FromSecondsSafe(settingsDeltaTime) && settingsSource.IsClassOnPreparingNotificationEnabled)
        {
            if (IsClassPreparingNotified)
                return;

            IsClassPreparingNotified = true;
            var deltaTime = CalculateDeltaTime(settingsDeltaTime, tClassDelta);
            var notificationRequest = _prepareOnClassNotificationRequest = BuildNotificationRequest(settingsSource, deltaTime);
            List<NotificationRequest> requests = [notificationRequest];
            if (settingsSource.IsClassOnNotificationEnabled)
            {
                var onClassNotificationRequest = _onClassNotificationRequest = BuildOnClassNotificationRequest(settingsSource);

                IsClassOnNotified = true;
                requests.Add(onClassNotificationRequest);
            }
            ShowChainedNotifications(requests.ToArray());

        }
        else if (IsClassPreparingNotified)
        {
            _prepareOnClassNotificationRequest?.Cancel();
            _prepareOnClassNotificationRequest = null;
            _onClassNotificationRequest = null;
            IsClassPreparingNotified = false;
        }
    }


    private IClassNotificationSettings GetEffectiveSettings()
    {
        var settings = GetAttachedSettingsNext();
        var isAttachedSettingsEnabled = settings?.IsAttachSettingsEnabled == true;
        return isAttachedSettingsEnabled ? settings! : Settings;
    }

    internal int GetSettingsDeltaTime()
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
        var deltaTime = TimeSpanHelper.FromSecondsSafe(settingsDeltaTime) - tClassDelta;
        return deltaTime > TimeSpan.FromSeconds(10) ? tClassDelta : TimeSpanHelper.FromSecondsSafe(settingsDeltaTime);
    }

    private NotificationRequest BuildNotificationRequest(IClassNotificationSettings settingsSource, TimeSpan deltaTime)
    {
        var message = GetNotificationMessage(settingsSource);

        var prepareOnClassNotificationRequest = new NotificationRequest
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
                SpeechContent = $"{message} 下节课是：{LessonsService.NextClassSubject.Name}{(Settings.ShowTeacherName ? $"，{FormatTeacher(LessonsService.NextClassSubject)}" : "")}。",
                EndTime = DateTimeToCurrentDateTimeConverter.Convert(LessonsService.NextClassTimeLayoutItem.StartSecond),
                IsSpeechEnabled = Settings.IsSpeechEnabledOnClassPreparing
            },
            ChannelId = Guid.Parse(PrepareOnClassChannelId)
        };
        
        //prepareOnClassNotificationRequest.CancellationToken.Register(prepareClassOnEndCallback);
        //prepareOnClassNotificationRequest.CompletedToken.Register(PrepareClassOnEndCallback);
        return prepareOnClassNotificationRequest;

        void PrepareClassOnEndCallback()
        {
            if (prepareOnClassNotificationRequest.CancellationToken.IsCancellationRequested)
            {
                return;
            }
            IsClassPreparingNotified = false;
        }
    }

    private string GetNotificationMessage(IClassNotificationSettings settingsSource)
    {
        if (settingsSource is null)
            return string.Empty;

        var message = Settings != settingsSource && settingsSource?.ClassOnPreparingText != null
            ? settingsSource!.ClassOnPreparingText
            : (LessonsService.NextClassSubject.IsOutDoor
                ? Settings.OutdoorClassOnPreparingText
                : Settings.ClassOnPreparingText);
        return message;
    }


    private void OnBreakingTime(object? sender, EventArgs e)
    {
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
        Channel(OnBreakingChannelId).ShowNotification(new NotificationRequest()
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
                SpeechContent = $"本节{LessonsService.CurrentTimeLayoutItem.BreakNameText}常{TimeSpanFormatHelper.Format(LessonsService.CurrentTimeLayoutItem.Last)}，下节课是：{LessonsService.NextClassSubject.Name}{(Settings.ShowTeacherName ? $"，{FormatTeacher(LessonsService.NextClassSubject)}" : "")}。{overlayText}",
                IsSpeechEnabled = Settings.IsSpeechEnabledOnClassOff
            }
        });
        
    }

    private void OnClass(object? sender, EventArgs e)
    {
        ShowOnClassNotificationCore();

        // 考虑到可能出现提醒过程中时间提前，导致提醒播放结束时还没上课导致重复提醒的情况，
        // 这里改为在上课时才取消准备上课提醒的标志。
        IsClassPreparingNotified = false;
        IsClassOnNotified = false;
    }

    private void ShowOnClassNotificationCore()
    {
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

        Channel(OnClassChannelId).ShowNotification(BuildOnClassNotificationRequest(settingsSource));
    }

    private NotificationRequest BuildOnClassNotificationRequest(IClassNotificationSettings settingsSource)
    {
        var classOnEndCallback = () =>
        {
            IsClassOnNotified = false;
        };
        var onClassNotificationRequest = new NotificationRequest()
        {
            MaskContent = NotificationContent.CreateTwoIconsMask(settingsSource.ClassOnMaskText,
                rightIcon: PackIconKind.Class, factory:
                x =>
                {
                    x.IsSpeechEnabled = Settings.IsSpeechEnabledOnClassOn;
                }),
            ChannelId = Guid.Parse(OnClassChannelId)
        };
        onClassNotificationRequest.CancellationToken.Register(classOnEndCallback);
        //onClassNotificationRequest.CompletedToken.Register(classOnEndCallback);
        return onClassNotificationRequest;
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
