using ClassIsland.Core.Abstractions;

namespace ClassIsland.Models.Notification;

public class NotificationConsumerRegisterInfo
{
    public INotificationConsumer Consumer { get; }
    public int Priority { get; }

    internal NotificationConsumerRegisterInfo(INotificationConsumer consumer, int priority)
    {
        Consumer = consumer;
        Priority = priority;
    }
}