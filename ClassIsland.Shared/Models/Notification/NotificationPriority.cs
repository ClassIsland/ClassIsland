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
        if (obj is not NotificationPriority other)
        {
            return -1;
        }

        // 插队优先
        if (IsPriorityOverride != other.IsPriorityOverride)
        {
            return IsPriorityOverride ? -1 : 1;
        }
        // 数字越小优先级越高
        if (Priority != other.Priority)
        {
            return Priority - other.Priority;
        }

        return Index - other.Index;
    }
}
