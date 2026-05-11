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
    /// 已取消（最终状态）
    /// </summary>
    Cancelled,
    /// <summary>
    /// 已正常完成（最终状态）
    /// </summary>
    Completed,
    /// <summary>
    /// 中断（等待移交到其他消费者继续播放）
    /// </summary>
    Interrupted
}