namespace ClassIsland.Shared.Models.Notification;

/// <summary>
/// 代表提醒队列的优先级顺序。
/// </summary>
public class NotificationPriority : IComparable
{
    /// <summary>
    /// 代表提醒队列的优先级顺序。
    /// </summary>
    internal NotificationPriority(int priority, int index, bool isPriorityOverride, bool isNotificationPlayed)
    {
        Priority = priority;
        Index = index;
        IsPriorityOverride = isPriorityOverride;
        IsNotificationPlayed = isNotificationPlayed;
    }

    /// <summary>
    /// 提醒提供方优先级。
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// 提醒插入队列的顺序。
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// 提醒是否插队。
    /// </summary>
    public bool IsPriorityOverride { get; }
    
    /// <summary>
    /// 提醒是否已经播放
    /// </summary>
    public bool IsNotificationPlayed { get; }

    /// <inheritdoc />
    public int CompareTo(object? obj)
    {
        if (obj is not NotificationPriority priority)
        {
            return -1;
        }
        
        if (IsPriorityOverride || IsNotificationPlayed)
        {
            return 1;
        }
        if (Priority != priority.Priority)
        {
            return Priority - priority.Priority;
        }

        return Index - priority.Index;
    }
}