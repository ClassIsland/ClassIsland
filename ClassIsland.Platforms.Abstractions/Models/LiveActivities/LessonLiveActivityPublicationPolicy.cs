namespace ClassIsland.Platforms.Abstraction.Models.LiveActivities;

/// <summary>
/// 决定课程实时活动是否已到达可见时间。
/// </summary>
internal static class LessonLiveActivityPublicationPolicy
{
    /// <summary>
    /// 让下一节课的进度从准备提醒时间开始，而不是从上一个课程区间开始。
    /// </summary>
    public static LessonLiveActivityContent AlignUpcomingProgressStart(
        LessonLiveActivityContent content,
        DateTimeOffset? preparationNotificationTime)
    {
        ArgumentNullException.ThrowIfNull(content);

        return content.IsUpcomingLesson &&
               preparationNotificationTime is { } preparationTime &&
               content.EndTime is { } endTime &&
               endTime > preparationTime
            ? content with { StartTime = preparationTime }
            : content;
    }

    /// <summary>
    /// 正在上课和课间的活动立即可见；下一节课倒计时必须等到准备上课提醒时间。
    /// </summary>
    public static bool ShouldPublish(
        LessonLiveActivityContent content,
        DateTimeOffset now,
        DateTimeOffset? preparationNotificationTime)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!content.ShouldBeVisible)
        {
            return false;
        }

        if (!content.IsUpcomingLesson)
        {
            return true;
        }

        return preparationNotificationTime is { } preparationTime &&
               now >= preparationTime;
    }
}
