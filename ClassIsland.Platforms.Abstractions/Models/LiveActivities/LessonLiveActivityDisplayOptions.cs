namespace ClassIsland.Platforms.Abstraction.Models.LiveActivities;

/// <summary>
/// 控制系统实时活动中显示的课程信息，不改变课程时间区间。
/// </summary>
public sealed record LessonLiveActivityDisplayOptions(
    bool ShowSubjectName = true,
    bool ShowRemainingTime = true,
    bool ShowTimeRange = true,
    bool ShowLocation = true,
    bool ShowTeacherName = true);
