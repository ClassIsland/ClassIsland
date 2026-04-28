using System.Collections.Generic;
using System.Linq;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Notification;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class NotificationRouter(ILogger<NotificationRouter> logger) : INotificationRouter
{
    private ILogger<NotificationRouter> Logger { get; } = logger;

    public NotificationConsumerRegisterInfo? Route(IReadOnlyList<NotificationPlayingTicket> requests, IReadOnlyList<NotificationConsumerRegisterInfo> consumers)
    {
        if (requests.Count <= 0)
            return null;

        var targetLine = requests.Count > 0
            ? requests[0].Request.TargetLineNumber
            : null;
        var consumer = consumers
            .FirstOrDefault(x => x.Consumer.AcceptsNotificationRequests && 
                                 x.Consumer.QueuedNotificationCount <= 0 &&
                                 (targetLine == null || x.LineNumber == targetLine));
        
        if (consumer != null)
        {
            Logger.LogTrace("路由结果：{}(#{})", consumer.Consumer, consumer.Consumer.GetHashCode());
        }
        else
        {
            Logger.LogTrace("找不到匹配的提醒消费者 (targetLine={})", targetLine);
        }

        return consumer;
    }
}
