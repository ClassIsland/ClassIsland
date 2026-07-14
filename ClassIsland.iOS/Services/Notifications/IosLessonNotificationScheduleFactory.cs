using System.Globalization;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models;
using ClassIsland.Models.AttachedSettings;
using ClassIsland.Models.NotificationProviderSettings;
using ClassIsland.Services;
using ClassIsland.Services.NotificationProviders;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Shared.Models.Notification;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// 将 ClassIsland 课表和提醒设置转换为可由 iOS 在应用挂起后触发的本地通知计划。
/// </summary>
internal sealed class IosLessonNotificationScheduleFactory(
    ILessonsService lessonsService,
    IProfileService profileService,
    INotificationHostService notificationHostService,
    SettingsService settingsService,
    IExactTimeService exactTimeService)
{
    internal const int MaximumPendingNotifications = 60;
    private const int MinimumPlanningHorizonDays = 7;
    private const int MaximumPlanningHorizonDays = 60;

    private static readonly Guid ProviderGuid = Guid.Parse(
        "08F0D9C3-C770-4093-A3D0-02F3D90C24BC");

    internal ClassNotificationSettings ProviderSettings { get; } =
        notificationHostService.GetNotificationProviderSettings<ClassNotificationSettings>(ProviderGuid);

    public IReadOnlyList<IosLessonNotificationRequest> Create()
    {
        if (!AreLessonNotificationsEnabled())
        {
            return [];
        }

        var providerSettings = ProviderSettings;
        var logicalNow = exactTimeService.GetCurrentLocalDateTime();
        var systemNow = DateTimeOffset.Now;
        var requests = new List<IosLessonNotificationRequest>();

        for (var dayOffset = 0; dayOffset < MaximumPlanningHorizonDays; dayOffset++)
        {
            var date = logicalNow.Date.AddDays(dayOffset);
            var classPlan = lessonsService.GetClassPlanByDate(date);
            if (classPlan is not { IsEnabled: true, TimeLayout: not null })
            {
                continue;
            }

            AddDayRequests(
                requests,
                classPlan,
                date,
                logicalNow,
                systemNow,
                providerSettings);

            if (dayOffset + 1 >= MinimumPlanningHorizonDays &&
                requests.Count(x => x.FireAt > systemNow.AddSeconds(1)) >=
                MaximumPendingNotifications)
            {
                break;
            }
        }

        return requests
            .Where(x => x.FireAt > systemNow.AddSeconds(1))
            .OrderBy(x => x.FireAt)
            .ThenBy(x => x.Identifier, StringComparer.Ordinal)
            .Take(MaximumPendingNotifications)
            .ToArray();
    }

    /// <summary>
    /// 获取当前下一节课的“准备上课”通知计划时间，供实时活动使用同一显示门槛。
    /// </summary>
    internal DateTimeOffset? GetUpcomingClassPreparationTime()
    {
        if (!AreLessonNotificationsEnabled())
        {
            return null;
        }

        var classPlan = lessonsService.CurrentClassPlan;
        var timeLayout = classPlan?.TimeLayout;
        var nextItem = lessonsService.NextClassTimeLayoutItem;
        if (classPlan is not { IsEnabled: true } ||
            timeLayout == null ||
            ReferenceEquals(nextItem, TimeLayoutItem.Empty) ||
            nextItem.EndTime <= nextItem.StartTime)
        {
            return null;
        }

        var subject = lessonsService.NextClassSubject;
        var attachedSettings = IAttachedSettingsHostService
            .GetAttachedSettingsByPriority<ClassNotificationAttachedSettings>(
                ProviderGuid,
                subject,
                nextItem,
                classPlan,
                timeLayout);
        IClassNotificationSettings effectiveSettings = attachedSettings is not null
            ? attachedSettings
            : ProviderSettings;
        var prepareDelivery = GetDeliveryOptions(
            ClassNotificationProvider.PrepareOnClassChannelId);
        var prepareDeltaSeconds = attachedSettings?.ClassPreparingDeltaTime ??
                                  (subject.IsOutDoor
                                      ? ProviderSettings.OutDoorClassPreparingDeltaTime
                                      : ProviderSettings.InDoorClassPreparingDeltaTime);
        if (!effectiveSettings.IsClassOnPreparingNotificationEnabled ||
            !prepareDelivery.Enabled ||
            prepareDeltaSeconds <= 0)
        {
            return null;
        }

        var logicalNow = exactTimeService.GetCurrentLocalDateTime();
        var systemNow = DateTimeOffset.Now;
        var startAt = IosNotificationTimeMapper.ToSystemTime(
            logicalNow.Date + nextItem.StartTime,
            logicalNow,
            systemNow);
        return startAt.AddSeconds(-prepareDeltaSeconds);
    }

    private bool AreLessonNotificationsEnabled()
    {
        var appSettings = settingsService.Settings;
        return appSettings.IsNotificationEnabled &&
               (!appSettings.NotificationProvidersEnableStates.TryGetValue(
                    ProviderGuid.ToString(),
                    out var providerEnabled) ||
                providerEnabled);
    }

    private void AddDayRequests(
        ICollection<IosLessonNotificationRequest> requests,
        ClassPlan classPlan,
        DateTime date,
        DateTime logicalNow,
        DateTimeOffset systemNow,
        ClassNotificationSettings providerSettings)
    {
        var timeLayout = classPlan.TimeLayout!;
        var validItems = classPlan.ValidTimeLayoutItems
            .Where(x => x.TimeType is 0 or 1)
            .OrderBy(x => x.StartTime)
            .ThenBy(x => x.EndTime)
            .ToArray();
        var allClassItems = timeLayout.Layouts
            .Where(x => x.TimeType == 0)
            .ToArray();
        var lessons = new List<LessonEntry>();

        foreach (var item in validItems.Where(x => x.TimeType == 0))
        {
            var classIndex = Array.IndexOf(allClassItems, item);
            if (classIndex < 0 ||
                classIndex >= classPlan.Classes.Count ||
                !classPlan.Classes[classIndex].IsEnabled ||
                item.EndTime <= item.StartTime)
            {
                continue;
            }

            profileService.Profile.Subjects.TryGetValue(
                classPlan.Classes[classIndex].SubjectId,
                out var subject);
            lessons.Add(new LessonEntry(classIndex, item, subject ?? Subject.Fallback));
        }

        foreach (var lesson in lessons)
        {
            var attachedSettings = IAttachedSettingsHostService
                .GetAttachedSettingsByPriority<ClassNotificationAttachedSettings>(
                    ProviderGuid,
                    lesson.Subject,
                    lesson.Item,
                    classPlan,
                    timeLayout);
            IClassNotificationSettings effectiveSettings = attachedSettings is not null
                ? attachedSettings
                : providerSettings;
            var subjectText = FormatSubject(
                lesson.Subject,
                providerSettings.ShowTeacherName);
            var startAt = IosNotificationTimeMapper.ToSystemTime(
                date + lesson.Item.StartTime,
                logicalNow,
                systemNow);
            var identifierPrefix = CreateIdentifierPrefix(
                date,
                lesson.Item,
                lesson.ClassIndex);

            var prepareDelivery = GetDeliveryOptions(
                ClassNotificationProvider.PrepareOnClassChannelId);
            var prepareDeltaSeconds = attachedSettings?.ClassPreparingDeltaTime ??
                                      (lesson.Subject.IsOutDoor
                                          ? providerSettings.OutDoorClassPreparingDeltaTime
                                          : providerSettings.InDoorClassPreparingDeltaTime);
            if (effectiveSettings.IsClassOnPreparingNotificationEnabled &&
                prepareDelivery.Enabled &&
                prepareDeltaSeconds > 0)
            {
                var prepareTitle = attachedSettings != null
                    ? attachedSettings.ClassOnPreparingMaskText
                    : lesson.Subject.IsOutDoor
                        ? providerSettings.OutdoorClassOnPreparingMaskText
                        : providerSettings.ClassOnPreparingMaskText;
                var prepareMessage = attachedSettings != null
                    ? attachedSettings.ClassOnPreparingText
                    : lesson.Subject.IsOutDoor
                        ? providerSettings.OutdoorClassOnPreparingText
                        : providerSettings.ClassOnPreparingText;
                var plannedPrepareAt = startAt.AddSeconds(-prepareDeltaSeconds);
                var catchUpFireAt = systemNow.AddSeconds(2);
                var isCatchUp = plannedPrepareAt <= systemNow.AddSeconds(1) &&
                                startAt > catchUpFireAt;
                requests.Add(new IosLessonNotificationRequest(
                    $"{identifierPrefix}.prepare",
                    isCatchUp ? catchUpFireAt : plannedPrepareAt,
                    EnsureText(prepareTitle, "即将上课"),
                    JoinBody(
                        prepareMessage,
                        $"下节课：{subjectText}，{FormatTime(lesson.Item.StartTime)} 开始。"),
                    prepareDelivery.PlaySound,
                    isCatchUp));
            }

            var onClassDelivery = GetDeliveryOptions(
                ClassNotificationProvider.OnClassChannelId);
            if (effectiveSettings.IsClassOnNotificationEnabled &&
                onClassDelivery.Enabled)
            {
                requests.Add(new IosLessonNotificationRequest(
                    $"{identifierPrefix}.on",
                    startAt,
                    EnsureText(effectiveSettings.ClassOnMaskText, "上课"),
                    $"{subjectText} · {FormatTime(lesson.Item.StartTime)}–{FormatTime(lesson.Item.EndTime)}",
                    onClassDelivery.PlaySound));
            }
        }

        foreach (var breakItem in validItems.Where(x => x.TimeType == 1))
        {
            var attachedSettings = IAttachedSettingsHostService
                .GetAttachedSettingsByPriority<ClassNotificationAttachedSettings>(
                    ProviderGuid,
                    timeLayoutItem: breakItem,
                    classPlan: classPlan,
                    timeLayout: timeLayout);
            IClassNotificationSettings effectiveSettings = attachedSettings is not null
                ? attachedSettings
                : providerSettings;
            var breakingDelivery = GetDeliveryOptions(
                ClassNotificationProvider.OnBreakingChannelId);
            if (!effectiveSettings.IsClassOffNotificationEnabled ||
                !breakingDelivery.Enabled)
            {
                continue;
            }

            var nextLesson = lessons.FirstOrDefault(
                x => x.Item.StartTime >= breakItem.StartTime);
            var body = nextLesson == null
                ? breakItem.BreakNameText
                : $"{breakItem.BreakNameText}。下一节：{FormatSubject(nextLesson.Subject, providerSettings.ShowTeacherName)}，{FormatTime(nextLesson.Item.StartTime)} 开始。";
            var layoutIndex = timeLayout.Layouts.IndexOf(breakItem);
            requests.Add(new IosLessonNotificationRequest(
                $"classisland.lessons.{date:yyyyMMdd}.{layoutIndex:D2}.break",
                IosNotificationTimeMapper.ToSystemTime(
                    date + breakItem.StartTime,
                    logicalNow,
                    systemNow),
                EnsureText(effectiveSettings.ClassOffMaskText, "课间休息"),
                body,
                breakingDelivery.PlaySound));
        }
    }

    private (bool Enabled, bool PlaySound) GetDeliveryOptions(string channelId)
    {
        var settings = settingsService.Settings;
        NotificationSettings? effectiveSettings = null;
        if (settings.NotificationChannelsNotifySettings.TryGetValue(
                Guid.Parse(channelId).ToString(),
                out var channelSettings) &&
            channelSettings.IsSettingsEnabled)
        {
            effectiveSettings = channelSettings;
        }
        else if (settings.NotificationProvidersNotifySettings.TryGetValue(
                     ProviderGuid.ToString(),
                     out var providerSettings) &&
                 providerSettings.IsSettingsEnabled)
        {
            effectiveSettings = providerSettings;
        }

        return (
            effectiveSettings?.IsNotificationEnabled ?? settings.IsNotificationEnabled,
            settings.AllowNotificationSound &&
            (effectiveSettings?.IsNotificationSoundEnabled ?? settings.IsNotificationSoundEnabled));
    }

    private static string CreateIdentifierPrefix(
        DateTime date,
        TimeLayoutItem item,
        int classIndex) =>
        $"classisland.lessons.{date:yyyyMMdd}.{classIndex:D2}.{item.StartTime.Ticks}";

    private static string FormatSubject(Subject subject, bool showTeacherName)
    {
        var name = ReferenceEquals(subject, Subject.Fallback) ||
                   string.IsNullOrWhiteSpace(subject.Name)
            ? "未命名课程"
            : subject.Name.Trim();
        return showTeacherName && !string.IsNullOrWhiteSpace(subject.TeacherName)
            ? $"{name}（{subject.TeacherName.Trim()}）"
            : name;
    }

    private static string FormatTime(TimeSpan time) =>
        time.ToString(@"hh\:mm", CultureInfo.InvariantCulture);

    private static string EnsureText(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string JoinBody(params string?[] parts) =>
        string.Join(
            Environment.NewLine,
            parts.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));

    private sealed record LessonEntry(
        int ClassIndex,
        TimeLayoutItem Item,
        Subject Subject);
}
