namespace ClassIsland.Platforms.Abstraction.Models.LiveActivities;

/// <summary>
/// 当前设备上的实时活动可用状态。
/// </summary>
public enum LiveActivityAvailability
{
    /// <summary>
    /// 当前平台或系统版本不支持实时活动。
    /// </summary>
    Unsupported = 0,

    /// <summary>
    /// 系统支持实时活动，但用户已将其关闭。
    /// </summary>
    Disabled = 1,

    /// <summary>
    /// 可以发布实时活动。
    /// </summary>
    Available = 2
}
