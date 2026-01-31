namespace ClassIsland.Core.Enums.Notification;

/// <summary>
/// 代表一个提醒的播放状态。
/// </summary>
public enum NotificationState
{
    /// <summary>
    /// 无
    /// </summary>
    None,
    /// <summary>
    /// 已加入队列
    /// </summary>
    Queued,
    /// <summary>
    /// 正在播放
    /// </summary>
    Playing,
    /// <summary>
    /// 已暂停
    /// </summary>
    Paused,
    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled,
    /// <summary>
    /// 已完成
    /// </summary>
    Completed
}