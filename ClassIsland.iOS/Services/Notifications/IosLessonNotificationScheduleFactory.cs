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
    IExactTimeService exactTimeService,
    LessonPreparationNotificationTimeline lessonPreparationTimeline)
{
    internal const int MaximumPendingNotifications = 60;
    private const int MaximumPlanningHorizonDays = 60;

    private static readonly Guid ProviderGuid =
        IosNotificationSchedulingPolicy.ClassNotificationProviderId;

    internal ClassNotificationSettings ProviderSettings { get; } =
        notificationHostService.GetNotificationProviderSettings<ClassNotificationSettings>(ProviderGuid);

    internal bool IsSchedulingEnabled
    {
        get
        {
            var appSettings = settingsService.Settings;
            var providerEnabled =
                !appSettings.NotificationProvidersEnableStates.TryGetValue(
                    ProviderGuid.ToString(),
                    out var configuredProviderEnabled) ||
                configuredProviderEnabled;
            return IosNotificationSchedulingPolicy.ShouldRequestAuthorization(
                appSettings.IsNotificationEnabled,
                providerEnabled,
                IosNotificationSchedulingPolicy.SupportedChannelIds.Select(
                    x => GetDeliveryOptions(x).Enabled));
        }
    }

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
        var preparationCandidates = new Dictionary<string, PreparationCandidate>(
            StringComparer.Ordinal);

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
                preparationCandidates,
                classPlan,
                date,
                logicalNow,
                systemNow,
                providerSettings);
        }

        var selectedRequests = IosLessonNotificationScheduleSelector.Select(
            requests.Where(x => x.FireAt > systemNow.AddSeconds(1)),
            MaximumPendingNotifications);
        var finalizedRequests = new List<IosLessonNotificationRequest>(
            selectedRequests.Count);
        foreach (var request in selectedRequests)
        {
            if (!preparationCandidates.TryGetValue(
                    request.Identifier,
                    out var preparationCandidate))
            {
                finalizedRequests.Add(request);
                continue;
            }

            var fireAt = lessonPreparationTimeline.PlanNotification(
                request.Identifier,
                preparationCandidate.PlannedPreparationAt,
                preparationCandidate.LessonStartAt,
                systemNow);
            if (fireAt is null)
            {
                continue;
            }

            finalizedRequests.Add(request with
            {
                FireAt = fireAt.Value,
                IsCatchUp = fireAt.Value != preparationCandidate.PlannedPreparationAt
            });
        }

        return finalizedRequests
            .OrderBy(x => x.FireAt)
            .ThenBy(x => x.Identifier, StringComparer.Ordinal)
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
        var allClassItems = timeLayout.Layouts
            .Where(x => x.TimeType == 0)
            .ToArray();
        var classIndex = Array.IndexOf(allClassItems, nextItem);
        if (classIndex < 0)
        {
            return null;
        }

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
        var plannedPrepareAt = startAt.AddSeconds(-prepareDeltaSeconds);
        var identifier = $"{CreateIdentifierPrefix(
            logicalNow.Date,
            nextItem,
            classIndex)}.prepare";
        return lessonPreparationTimeline.GetLiveActivityPublicationTime(
            identifier,
            plannedPrepareAt,
            startAt,
            systemNow);
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
        IDictionary<string, PreparationCandidate> preparationCandidates,
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
                var identifier = $"{identifierPrefix}.prepare";
                var candidateFireAt = lessonPreparationTimeline.GetCandidateNotificationTime(
                    plannedPrepareAt,
                    startAt,
                    systemNow);
                if (candidateFireAt is { } fireAt)
                {
                    preparationCandidates[identifier] = new PreparationCandidate(
                        plannedPrepareAt,
                        startAt);
                    requests.Add(new IosLessonNotificationRequest(
                        identifier,
                        fireAt,
                        EnsureText(prepareTitle, "即将上课"),
                        JoinBody(
                            prepareMessage,
                            $"下节课：{subjectText}，{FormatTime(lesson.Item.StartTime)} 开始。"),
                        IosNotificationSchedulingPolicy.PrepareOnClassChannelId,
                        prepareDelivery.PlaySound,
                        fireAt != plannedPrepareAt,
                        identifierPrefix));
                }
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
                    IosNotificationSchedulingPolicy.OnClassChannelId,
                    onClassDelivery.PlaySound,
                    ChainId: identifierPrefix));
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
                IosNotificationSchedulingPolicy.OnBreakingChannelId,
                breakingDelivery.PlaySound));
        }
    }

    private (bool Enabled, bool PlaySound) GetDeliveryOptions(string channelId) =>
        GetDeliveryOptions(Guid.Parse(channelId));

    private (bool Enabled, bool PlaySound) GetDeliveryOptions(Guid channelId)
    {
        var settings = settingsService.Settings;
        NotificationSettings? effectiveSettings = null;
        if (settings.NotificationChannelsNotifySettings.TryGetValue(
                channelId.ToString(),
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

    private sealed record PreparationCandidate(
        DateTimeOffset PlannedPreparationAt,
        DateTimeOffset LessonStartAt);

    private sealed record LessonEntry(
        int ClassIndex,
        TimeLayoutItem Item,
        Subject Subject);
}
