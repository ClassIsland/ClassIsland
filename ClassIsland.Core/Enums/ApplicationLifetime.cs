namespace ClassIsland.Core.Enums;

/// <summary>
/// 表示应用生命周期状态
/// </summary>
public enum ApplicationLifetime
{
    /// <summary>
    /// 无
    /// </summary>
    None,
    /// <summary>
    /// 早期启动（无 WPF）
    /// </summary>
    EarlyLoading,
    /// <summary>
    /// 初始化应用（有 WPF,无应用主机）
    /// </summary>
    Initializing,
    /// <summary>
    /// 启动中
    /// </summary>
    Starting,
    /// <summary>
    /// 正常工作中
    /// </summary>
    Running,
    /// <summary>
    /// 停止应用中
    /// </summary>
    Stopping,
}