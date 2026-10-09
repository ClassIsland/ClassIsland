namespace ClassIsland.Platforms.Abstraction.Models.LiveActivities;

/// <summary>
/// 结束实时活动后的系统移除策略。
/// </summary>
public enum LiveActivityDismissalPolicy
{
    /// <summary>
    /// 由系统决定何时从锁屏移除已结束的活动。
    /// </summary>
    Default = 0,

    /// <summary>
    /// 结束后立即移除活动。
    /// </summary>
    Immediate = 1
}
