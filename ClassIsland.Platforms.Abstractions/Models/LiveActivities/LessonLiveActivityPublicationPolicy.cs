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

        return IsPreparationPhase(content) &&
               preparationNotificationTime is { } preparationTime &&
               content.EndTime is { } endTime &&
               endTime > preparationTime
            ? content with { StartTime = preparationTime }
            : content;
    }

    /// <summary>
    /// 正在上课和普通课间的活动立即可见；只有课前准备阶段的
    /// “下一节课”倒计时需要等到准备上课提醒时间。
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

        if (!IsPreparationPhase(content))
        {
            return true;
        }

        return preparationNotificationTime is { } preparationTime &&
               now >= preparationTime;
    }

    private static bool IsPreparationPhase(LessonLiveActivityContent content) =>
        content.IsUpcomingLesson && content.Phase == LessonLiveActivityPhase.None;
}
