using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Enums.Notification;
using ClassIsland.Core.Models.Notification;

namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// iOS 使用系统本地通知显示课程提醒；此消费者只负责完成桌面提醒票据，
/// 避免没有桌面时间线消费者时请求在内存队列中持续累积。
/// </summary>
internal sealed class IosNotificationQueueConsumer : INotificationConsumer
{
    public int QueuedNotificationCount => 0;

    public bool AcceptsNotificationRequests => true;

    public void ReceiveNotifications(
        IReadOnlyList<NotificationPlayingTicket> notificationRequests)
    {
        foreach (var ticket in notificationRequests)
        {
            ticket.Request.State = NotificationState.Completed;
            try
            {
                ticket.Request.CompletedTokenSource.Cancel();
            }
            catch (AggregateException exception)
            {
                // CancellationToken 回调由提醒提供方拥有；单个回调异常不能阻止
                // 其余票据从 iOS 的桌面提醒队列中完成。
                Console.Error.WriteLine($"完成 iOS 桌面提醒票据时回调失败：{exception}");
            }
        }
    }
}
