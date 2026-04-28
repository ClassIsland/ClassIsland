using ClassIsland.Core.Abstractions;

namespace ClassIsland.Core.Models.Notification;

/// <summary>
/// 提醒消费者注册信息。
/// </summary>
/// <remarks>
/// 初始化 <see cref="NotificationConsumerRegisterInfo"/> 类的新实例。
/// </remarks>
/// <param name="consumer">提醒消费者。</param>
/// <param name="priority">优先级。</param>
/// <param name="lineNumber">行号。</param>
public class NotificationConsumerRegisterInfo(INotificationConsumer consumer, int priority, int? lineNumber = null)
{
    /// <summary>
    /// 提醒消费者。
    /// </summary>
    public INotificationConsumer Consumer { get; } = consumer;

    /// <summary>
    /// 优先级。
    /// </summary>
    public int Priority { get; } = priority;

    /// <summary>
    /// 行号。
    /// </summary>
    public int? LineNumber { get; } = lineNumber;
}
