namespace ClassIsland.Platforms.Abstraction.Models.LiveActivities;

/// <summary>
/// 课程实时活动当前所处的阶段。
/// </summary>
public enum LessonLiveActivityPhase
{
    /// <summary>
    /// 当前没有课程。
    /// </summary>
    None = 0,

    /// <summary>
    /// 正在上课。
    /// </summary>
    OnClass = 1,

    /// <summary>
    /// 正在课间休息。
    /// </summary>
    Breaking = 2,

    /// <summary>
    /// 当天课程已经结束。
    /// </summary>
    AfterSchool = 3
}
