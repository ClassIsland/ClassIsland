namespace ClassIsland.Platforms.Abstraction.Models.LiveActivities;

/// <summary>
/// 要发布到系统实时活动表面的课程内容。
/// </summary>
/// <param name="IntervalId">当前课程区间的稳定业务标识，用于日志和内容去重。</param>
/// <param name="Phase">当前课程阶段。</param>
/// <param name="Title">主要标题。</param>
/// <param name="Subtitle">次要说明。</param>
/// <param name="Detail">锁屏展开区域使用的详细文本。</param>
/// <param name="CompactText">灵动岛紧凑区域使用的短文本。</param>
/// <param name="StartTime">进度开始时间；没有时间进度时为 <see langword="null"/>。</param>
/// <param name="EndTime">进度结束时间；没有时间进度时为 <see langword="null"/>。</param>
/// <param name="DeepLink">点击实时活动时打开的应用 URI。</param>
/// <param name="IsUpcomingLesson">内容是否表示尚未开始的下一节课。</param>
public sealed record LessonLiveActivityContent(
    string IntervalId,
    LessonLiveActivityPhase Phase,
    string Title,
    string Subtitle,
    string Detail,
    string CompactText,
    DateTimeOffset? StartTime = null,
    DateTimeOffset? EndTime = null,
    string DeepLink = "classisland://app/live-activity",
    bool IsUpcomingLesson = false)
{
    /// <summary>
    /// 当前内容是否包含可由系统持续渲染的时间进度。
    /// </summary>
    public bool HasProgress => StartTime is { } start && EndTime is { } end && end > start;

    /// <summary>
    /// 当前内容是否对应正在进行或即将开始的课程区间。
    /// </summary>
    public bool ShouldBeVisible =>
        Phase is LessonLiveActivityPhase.OnClass or LessonLiveActivityPhase.Breaking ||
        Phase == LessonLiveActivityPhase.None && HasProgress;
}
