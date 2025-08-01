namespace ClassIsland.Shared.Enums;

/// <summary>
/// 行动组状态。
/// </summary>
public enum ActionSetStatus
{
    /// <summary>
    /// 正常。
    /// </summary>
    Normal,
    /// <summary>
    /// 正在执行触发。
    /// </summary>
    Invoking,
    /// <summary>
    /// 等待恢复。
    /// </summary>
    On,
    /// <summary>
    /// 正在执行恢复。
    /// </summary>
    Reverting,
}