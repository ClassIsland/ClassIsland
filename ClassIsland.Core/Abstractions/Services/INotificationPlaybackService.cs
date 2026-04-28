using System.Collections.Generic;
using ClassIsland.Core.Models.Notification;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 提醒播放服务接口，负责协调提醒的播放流程。
/// </summary>
public interface INotificationPlaybackService
{
    /// <summary>
    /// 为指定的消费者入队提醒票据并开始播放（如果尚未开始）。
    /// </summary>
    void EnqueueAndPlay(INotificationConsumer consumer, INotificationPlaybackHandler handler, IEnumerable<NotificationPlayingTicket> tickets);

    /// <summary>
    /// 获取指定消费者的待播放提醒数量。
    /// </summary>
    int GetQueuedCount(INotificationConsumer consumer);

    /// <summary>
    /// 取消指定消费者的所有提醒播放。
    /// </summary>
    void CancelAll(INotificationConsumer consumer);
}
