namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 协调准备上课补发通知与实时活动使用同一个发布时间。
/// </summary>
internal sealed class LessonPreparationNotificationTimeline
{
    private static readonly TimeSpan SchedulingSafetyMargin = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan CatchUpDelay = TimeSpan.FromSeconds(2);

    private readonly object _syncRoot = new();
    private readonly Dictionary<string, PreparationTiming> _preparationTimings = new(StringComparer.Ordinal);

    /// <summary>
    /// 由通知计划器登记有效触发时间。错过原计划时只登记一次补发时间。
    /// </summary>
    public DateTimeOffset? PlanNotification(
        string notificationIdentifier,
        DateTimeOffset plannedPreparationAt,
        DateTimeOffset lessonStartAt,
        DateTimeOffset systemNow)
    {
        ValidateIdentifier(notificationIdentifier);
        if (lessonStartAt <= plannedPreparationAt || lessonStartAt <= systemNow)
        {
            return null;
        }

        lock (_syncRoot)
        {
            RemoveExpiredTimings(systemNow);
            if (plannedPreparationAt > systemNow + SchedulingSafetyMargin)
            {
                RememberPlannedTiming(
                    notificationIdentifier,
                    plannedPreparationAt,
                    lessonStartAt,
                    plannedPreparationAt);
                return plannedPreparationAt;
            }

            if (_preparationTimings.TryGetValue(notificationIdentifier, out var existing) &&
                existing.PlannedPreparationAt == plannedPreparationAt &&
                existing.LessonStartAt == lessonStartAt)
            {
                if (existing.IsScheduled ||
                    existing.FireAt > systemNow + SchedulingSafetyMargin)
                {
                    return existing.FireAt;
                }
            }

            var catchUpFireAt = CeilingToWholeSecond(systemNow + CatchUpDelay);
            if (lessonStartAt <= catchUpFireAt)
            {
                return null;
            }

            RememberPlannedTiming(
                notificationIdentifier,
                plannedPreparationAt,
                lessonStartAt,
                catchUpFireAt);
            return catchUpFireAt;
        }
    }

    /// <summary>
    /// 获取实时活动门槛。补发场景必须等待通知计划器先登记时间。
    /// </summary>
    public DateTimeOffset? GetLiveActivityPublicationTime(
        string notificationIdentifier,
        DateTimeOffset plannedPreparationAt,
        DateTimeOffset lessonStartAt,
        DateTimeOffset systemNow)
    {
        ValidateIdentifier(notificationIdentifier);
        if (lessonStartAt <= plannedPreparationAt || lessonStartAt <= systemNow)
        {
            return null;
        }

        if (plannedPreparationAt > systemNow + SchedulingSafetyMargin)
        {
            return plannedPreparationAt;
        }

        lock (_syncRoot)
        {
            RemoveExpiredTimings(systemNow);
            return _preparationTimings.TryGetValue(notificationIdentifier, out var timing) &&
                   timing.IsScheduled
                ? timing.FireAt
                : null;
        }
    }

    /// <summary>
    /// 仅保留本轮仍会交给系统调度的准备提醒。
    /// </summary>
    public void ReconcileScheduledNotifications(
        IEnumerable<string> notificationIdentifiers,
        DateTimeOffset systemNow)
    {
        ArgumentNullException.ThrowIfNull(notificationIdentifiers);
        var identifiers = notificationIdentifiers.ToHashSet(StringComparer.Ordinal);
        lock (_syncRoot)
        {
            RemoveExpiredTimings(systemNow);
            foreach (var identifier in _preparationTimings.Keys
                         .Where(x => !identifiers.Contains(x) &&
                                     !_preparationTimings[x].IsScheduled)
                         .ToArray())
            {
                _preparationTimings.Remove(identifier);
            }
        }
    }

    /// <summary>
    /// 在 UNUserNotificationCenter 接受请求后，开放对应实时活动门槛。
    /// </summary>
    public void ConfirmNotificationScheduled(
        string notificationIdentifier,
        DateTimeOffset fireAt)
    {
        ValidateIdentifier(notificationIdentifier);
        lock (_syncRoot)
        {
            if (_preparationTimings.TryGetValue(notificationIdentifier, out var timing) &&
                timing.FireAt == fireAt)
            {
                _preparationTimings[notificationIdentifier] = timing with
                {
                    IsScheduled = true
                };
            }
        }
    }

    /// <summary>
    /// 从系统现有或已送达的通知恢复跨进程丢失的确认状态。
    /// </summary>
    public void RestoreNotificationScheduled(
        string notificationIdentifier,
        DateTimeOffset fireAt)
    {
        ValidateIdentifier(notificationIdentifier);
        lock (_syncRoot)
        {
            if (_preparationTimings.TryGetValue(notificationIdentifier, out var timing) &&
                fireAt < timing.LessonStartAt)
            {
                _preparationTimings[notificationIdentifier] = timing with
                {
                    FireAt = fireAt,
                    IsScheduled = true
                };
            }
        }
    }

    private void RememberPlannedTiming(
        string notificationIdentifier,
        DateTimeOffset plannedPreparationAt,
        DateTimeOffset lessonStartAt,
        DateTimeOffset fireAt)
    {
        if (_preparationTimings.TryGetValue(notificationIdentifier, out var existing) &&
            existing.PlannedPreparationAt == plannedPreparationAt &&
            existing.LessonStartAt == lessonStartAt &&
            existing.FireAt == fireAt)
        {
            return;
        }

        _preparationTimings[notificationIdentifier] = new PreparationTiming(
            plannedPreparationAt,
            lessonStartAt,
            fireAt,
            IsScheduled: false);
    }

    private void RemoveExpiredTimings(DateTimeOffset systemNow)
    {
        foreach (var identifier in _preparationTimings
                     .Where(x => x.Value.LessonStartAt <= systemNow)
                     .Select(x => x.Key)
                     .ToArray())
        {
            _preparationTimings.Remove(identifier);
        }
    }

    private static void ValidateIdentifier(string notificationIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(notificationIdentifier);
    }

    private static DateTimeOffset CeilingToWholeSecond(DateTimeOffset value)
    {
        var remainder = value.Ticks % TimeSpan.TicksPerSecond;
        return remainder == 0
            ? value
            : value.AddTicks(TimeSpan.TicksPerSecond - remainder);
    }

    private sealed record PreparationTiming(
        DateTimeOffset PlannedPreparationAt,
        DateTimeOffset LessonStartAt,
        DateTimeOffset FireAt,
        bool IsScheduled);
}
