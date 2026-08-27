using System;
using System.Collections.Generic;
using ClassIsland.Core.Enums.Notification;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Core.Abstractions;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 通知事件总线
/// </summary>
public interface INotificationBus
{
    /// <summary>
    /// 通知状态变更事件
    /// </summary>
    event Action<NotificationRequest, NotificationState, NotificationState>? StateChanged;

    /// <summary>
    /// 请求拉取事件
    /// </summary>
    event Action<INotificationConsumer>? ConsumerBecameIdle;

    /// <summary>
    /// 消费者移除事件
    /// </summary>
    event Action<INotificationConsumer>? ConsumerRemoved;

    /// <summary>
    /// 发布状态变更事件
    /// </summary>
    void RaiseStateChanged(NotificationRequest request, NotificationState from, NotificationState to);

    /// <summary>
    /// 发布拉取请求事件并返回结果
    /// </summary>
    void RaiseConsumerBecameIdle(INotificationConsumer consumer);

    /// <summary>
    /// 发布消费者移除事件
    /// </summary>
    void RaiseConsumerRemoved(INotificationConsumer consumer);
}
