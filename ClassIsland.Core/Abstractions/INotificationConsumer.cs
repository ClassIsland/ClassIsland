using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Notification;

namespace ClassIsland.Core.Abstractions;

/// <summary>
/// 提醒消费者，负责处理提醒。
/// </summary>
public interface INotificationConsumer
{
    /// <summary>
    /// 向提醒消费者主动推送提醒。
    /// </summary>
    /// <remarks>
    /// 此方法一般情况下只会推送一个要显示的提醒。如果有成链的提醒，会将这些提醒一并推送。此方法只会在当前提醒消费者的提醒全部播放完毕
    /// （<see cref="QueuedNotificationCount"/> = 0）的情况下才会调用。如果您要连续播放提醒，请在提醒播放完后手动调用
    /// <see cref="INotificationHostService.PullNotificationRequests"/> 手动拉取后续提醒。
    /// </remarks>
    /// <param name="notificationRequests">推送的提醒</param>
    public void ReceiveNotifications(IReadOnlyList<NotificationRequest> notificationRequests);
    
    /// <summary>
    /// 待处理的提醒数量
    /// </summary>
    public int QueuedNotificationCount { get; }
    
    /// <summary>
    /// 是否接受提醒请求。如果为 false，提醒主机将不会像此消费者推送提醒请求。
    /// </summary>
    public bool AcceptsNotificationRequests { get; }
}