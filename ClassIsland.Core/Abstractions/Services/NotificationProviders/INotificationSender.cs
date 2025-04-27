using ClassIsland.Core.Models.Notification;

namespace ClassIsland.Core.Abstractions.Services.NotificationProviders;

/// <summary>
/// 代表提醒发送方
/// </summary>
public interface INotificationSender
{
    /// <summary>
    /// 显示一个提醒。
    /// </summary>
    /// <param name="request">提醒请求</param>
    void ShowNotification(NotificationRequest request);

    /// <summary>
    /// 显示一个提醒，并等待提醒显示完成。
    /// </summary>
    /// <param name="request">提醒请求</param>
    Task ShowNotificationAsync(NotificationRequest request);

    /// <summary>
    /// 显示链式提醒。链式显示的提醒会按照传入的顺序显示，并且当其中一个提醒被取消时，所有后续的提醒都会被取消。
    /// </summary>
    /// <param name="requests">提醒请求</param>
    void ShowChainedNotifications(params NotificationRequest[] requests);

    /// <summary>
    /// 显示链式提醒，并等待最后一个提醒显示完成。链式显示的提醒会按照传入的顺序显示，并且当其中一个提醒被取消时，所有后续的提醒都会被取消。
    /// </summary>
    /// <param name="requests">提醒请求</param>
    Task ShowChainedNotificationsAsync(NotificationRequest[] requests);
}