using System;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models;
using ClassIsland.Models.AttachedSettings;
using ClassIsland.Platforms.Abstraction.Models.LiveActivities;

namespace ClassIsland.Services.LiveActivities;

internal static class LessonLiveActivitySettingsPolicy
{
    public static IRealtimeNotificationSettings GetEffectiveSettings(
        Settings settings,
        ILessonsService lessonsService,
        bool isUpcoming)
    {
        var attached = IAttachedSettingsHostService.GetAttachedSettingsByPriority<RealtimeNotificationAttachedSettings>(
            Guid.Parse(RealtimeNotificationAttachedSettings.SettingsId),
            isUpcoming ? lessonsService.NextClassSubject : lessonsService.CurrentSubject,
            isUpcoming ? lessonsService.NextClassTimeLayoutItem : lessonsService.CurrentTimeLayoutItem,
            lessonsService.CurrentClassPlan,
            lessonsService.CurrentClassPlan?.TimeLayout);
        return (IRealtimeNotificationSettings?)attached ?? settings;
    }

    public static LessonLiveActivityDisplayOptions GetDisplayOptions(
        IRealtimeNotificationSettings settings,
        bool isUpcoming) => isUpcoming
        ? new LessonLiveActivityDisplayOptions(
            settings.UpcomingClassRealtimeNotificationShowSubjectName,
            settings.UpcomingClassRealtimeNotificationShowRemainingTime,
            settings.UpcomingClassRealtimeNotificationShowTimeRange,
            settings.UpcomingClassRealtimeNotificationShowLocation,
            settings.UpcomingClassRealtimeNotificationShowTeacherName)
        : new LessonLiveActivityDisplayOptions(
            settings.CurrentClassRealtimeNotificationShowSubjectName,
            settings.CurrentClassRealtimeNotificationShowRemainingTime,
            settings.CurrentClassRealtimeNotificationShowTimeRange,
            settings.CurrentClassRealtimeNotificationShowLocation,
            settings.CurrentClassRealtimeNotificationShowTeacherName);

    public static DateTimeOffset? GetPublicationTime(
        LessonLiveActivityContent content,
        IRealtimeNotificationSettings settings) =>
        content.IsUpcomingLesson &&
        settings.IsUpcomingClassRealtimeNotificationEnabled &&
        settings.UpcomingClassRealtimeNotificationLeadMinutes > 0 &&
        content.EndTime is { } endTime
            ? endTime.AddMinutes(-settings.UpcomingClassRealtimeNotificationLeadMinutes)
            : null;

    public static bool ShouldPublish(
        LessonLiveActivityContent content,
        IRealtimeNotificationSettings settings,
        DateTimeOffset now)
    {
        if (!content.ShouldBeVisible)
        {
            return false;
        }

        return content.IsUpcomingLesson
            ? GetPublicationTime(content, settings) is { } publicationTime &&
              content.EndTime is { } endTime && now >= publicationTime && now < endTime
            : settings.IsCurrentClassRealtimeNotificationEnabled;
    }
}
