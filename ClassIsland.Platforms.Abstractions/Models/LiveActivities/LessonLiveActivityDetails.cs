namespace ClassIsland.Platforms.Abstraction.Models.LiveActivities;

/// <summary>
/// 由课程服务准备的实时活动展示字段，原生视图不解析课表或图标表达式。
/// </summary>
/// <param name="LessonName">当前或下一节课的科目名。</param>
/// <param name="Location">科目设置中的上课地点。</param>
/// <param name="TimeText">该节课的本地时间范围。</param>
/// <param name="IntervalTimeText">当前课间的本地时间范围，没有时为空。</param>
/// <param name="IconPngBase64">已由应用渲染的透明 PNG 图标。</param>
public sealed record LessonLiveActivityDetails(
    string LessonName,
    string Location,
    string TimeText,
    string IntervalTimeText,
    string? IconPngBase64 = null);
