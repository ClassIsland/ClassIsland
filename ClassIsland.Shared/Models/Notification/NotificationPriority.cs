namespace ClassIsland.Shared.Models.Notification;

/// <summary>
/// 代表提醒队列的优先级顺序。
/// </summary>
public class NotificationPriority(int priority, int index, bool isPriorityOverride) : IComparable
{
    /// <summary>
    /// 提醒提供方优先级。
    /// </summary>
    public int Priority { get; } = priority;

    /// <summary>
    /// 提醒插入队列的顺序。
    /// </summary>
    public int Index { get; } = index;

    /// <summary>
    /// 提醒是否插队。
    /// </summary>
    public bool IsPriorityOverride { get; } = isPriorityOverride;

    /// <inheritdoc />
    public int CompareTo(object? obj)
    {
        if (obj is not NotificationPriority priority)
        {
            return -1;
        }

        if (IsPriorityOverride)
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