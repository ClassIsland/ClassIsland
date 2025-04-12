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

public class ClassOffNotificationProvider : INotificationProvider, IHostedService
{
    public string Name { get; set; } = "下课提醒";
    public string Description { get; set; } = "在下课时发出醒目提醒，并预告下一节课程。";
    public Guid ProviderGuid { get; set; } = new Guid("D36D0B6B-DBEC-23DD-EF2B-F313C419A16E");
    public object? SettingsElement { get; set; }
    public object? IconElement { get; set; } = new PackIcon()
    {
        Kind = PackIconKind.Notifications,
        Width = 24,
        Height = 24
    };

    private ClassOffNotificationSettings Settings
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

    public ClassOffNotificationProvider(INotificationHostService notificationHostService, IAttachedSettingsHostService attachedSettingsHostService , ILessonsService lessonsService, IExactTimeService exactTimeService)
    {
        NotificationHostService = notificationHostService;
        LessonsService = lessonsService;
        ExactTimeService = exactTimeService;

        NotificationHostService.RegisterNotificationProvider(this);
        LessonsService.OnBreakingTime += OnBreakingTime;
        Settings = NotificationHostService.GetNotificationProviderSettings
                       <ClassOffNotificationSettings>(ProviderGuid);
        SettingsElement = new ClassOffNotificationProviderSettingsControl(Settings);

        var item = typeof(ClassOffNotificationAttachedSettingsControl);
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

        if (LessonsService.NextClassSubject != Subject.Empty)
        {
            NotificationHostService.ShowNotification(new NotificationRequest()
            {
                MaskContent = new ClassOffNotificationProviderControl("ClassOffNotification")
                {
                    ShowTeacherNameWhenClassOff = Settings.ShowTeacherNameWhenClassOff
                },
                MaskDuration = TimeSpan.FromSeconds(2),
                MaskSpeechContent = LessonsService.CurrentTimeLayoutItem.BreakNameText,
                OverlayContent = new ClassOffNotificationProviderControl("ClassOffOverlay")
                {
                    ShowTeacherNameWhenClassOff = Settings.ShowTeacherNameWhenClassOff,
                    Message = overlayText
                },
                OverlayDuration = showOverlayText ? TimeSpan.FromSeconds(20) : TimeSpan.FromSeconds(10),
                OverlaySpeechContent = $"本节{LessonsService.CurrentTimeLayoutItem.BreakNameText}常{TimeSpanFormatHelper.Format(LessonsService.CurrentTimeLayoutItem.Last)}，下节课是：{LessonsService.NextClassSubject.Name} {(Settings.ShowTeacherNameWhenClassOff ? FormatTeacher(LessonsService.NextClassSubject) : "")}。{overlayText}",
                IsSpeechEnabled = Settings.IsSpeechEnabledOnClassOff
            });
        }
        else
        {
            NotificationHostService.ShowNotification(new NotificationRequest()
            {
                MaskContent = new ClassOffNotificationProviderControl("ClassOffNotification")
                {
                    ShowTeacherNameWhenClassOff = Settings.ShowTeacherNameWhenClassOff
                },
                MaskDuration = TimeSpan.FromSeconds(5),
                MaskSpeechContent = LessonsService.CurrentTimeLayoutItem.BreakNameText,
                IsSpeechEnabled = Settings.IsSpeechEnabledOnClassOff
            });
        }
    }

    private ClassOffNotificationAttachedSettings? GetAttachedSettings()
    {
        var mvm = App.GetService<MainWindow>().ViewModel;
        var settings = IAttachedSettingsHostService
            .GetAttachedSettingsByPriority<
                ClassOffNotificationAttachedSettings>(
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
