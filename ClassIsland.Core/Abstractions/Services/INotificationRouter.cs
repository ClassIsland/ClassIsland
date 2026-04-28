using ClassIsland.Core.Models.Notification;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 提醒路由接口，负责决定提醒应该分发给哪个消费者。
/// </summary>
public interface INotificationRouter
{
    /// <summary>
    /// 路由提醒。
    /// </summary>
    /// <param name="requests">待分发的提醒票据列表。</param>
    /// <param name="consumers">已注册的消费者列表。</param>
    /// <returns>选中的消费者，如果没有合适的消费者则返回 null。</returns>
    NotificationConsumerRegisterInfo? Route(IReadOnlyList<NotificationPlayingTicket> requests, IReadOnlyList<NotificationConsumerRegisterInfo> consumers);
}
