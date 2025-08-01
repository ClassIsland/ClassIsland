using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Shared.Abstraction.Models;
using ClassIsland.Models.AttachedSettings;
using ClassIsland.Models.NotificationProviderSettings;

namespace ClassIsland.Services.NotificationProviders;

[NotificationProviderInfo("8FBC3A26-6D20-44DD-B895-B9411E3DDC51", "放学提醒", "\ued34", "在当天的课程结束后发出提醒。")]
public class AfterSchoolNotificationProvider : NotificationProviderBase<AfterSchoolNotificationProviderSettings>
{
    public INotificationHostService NotificationHostService { get; }

    private ILessonsService LessonsService { get; }

    private IExactTimeService ExactTimeService { get; }

    public AfterSchoolNotificationProvider(INotificationHostService notificationHostService, IAttachedSettingsHostService attachedSettingsHostService, ILessonsService lessonsService, IExactTimeService exactTimeService)
    {
        NotificationHostService = notificationHostService;
        LessonsService = lessonsService;
        ExactTimeService = exactTimeService;
        LessonsService.OnAfterSchool += OnAfterSchool;

    }

    private void OnAfterSchool(object? sender, EventArgs e)
    {
        var settings = (IAfterSchoolNotificationProviderSettingsBase?)GetAttachedSettings() ?? Settings;
        var now = ExactTimeService.GetCurrentLocalDateTime().TimeOfDay;
        var afterSchoolTime = LessonsService.CurrentClassPlan?.ValidTimeLayoutItems.LastOrDefault(i => i.TimeType is 0 or 1)?.EndTime;
        if (!settings.IsEnabled ||
            (now - afterSchoolTime) > TimeSpan.FromSeconds(10))
            return;

        ShowNotification(new NotificationRequest
        {
            MaskContent = NotificationContent.CreateTwoIconsMask("放学", rightIcon: "\ued34"),
            OverlayContent = NotificationContent.CreateSimpleTextContent(settings.NotificationMsg, x => x.Duration=TimeSpan.FromSeconds(30))
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