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
    /// 当实时活动可使用的准备提醒发布时间发生变化时触发。
    /// </summary>
    public event EventHandler? PublicationTimeChanged;

    /// <summary>
    /// 无副作用地计算一次新准备提醒应使用的候选时间。
    /// </summary>
    public DateTimeOffset? GetCandidateNotificationTime(
        DateTimeOffset plannedPreparationAt,
        DateTimeOffset lessonStartAt,
        DateTimeOffset systemNow)
    {
        if (lessonStartAt <= plannedPreparationAt || lessonStartAt <= systemNow)
        {
            return null;
        }

        if (plannedPreparationAt > systemNow + SchedulingSafetyMargin)
        {
            return plannedPreparationAt;
        }

        var catchUpFireAt = CeilingToWholeSecond(systemNow + CatchUpDelay);
        return lessonStartAt > catchUpFireAt ? catchUpFireAt : null;
    }

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

        var candidateFireAt = GetCandidateNotificationTime(
            plannedPreparationAt,
            lessonStartAt,
            systemNow);

        DateTimeOffset? fireAt;
        var publicationTimeChanged = false;
        lock (_syncRoot)
        {
            RemoveExpiredTimings(systemNow);
            if (candidateFireAt == plannedPreparationAt)
            {
                publicationTimeChanged = RememberPlannedTiming(
                    notificationIdentifier,
                    plannedPreparationAt,
                    lessonStartAt,
                    plannedPreparationAt);
                fireAt = plannedPreparationAt;
            }
            else if (_preparationTimings.TryGetValue(notificationIdentifier, out var existing) &&
                     existing.PlannedPreparationAt == plannedPreparationAt &&
                     existing.LessonStartAt == lessonStartAt &&
                     (existing.IsScheduled ||
                      existing.FireAt > systemNow + SchedulingSafetyMargin))
            {
                fireAt = existing.FireAt;
            }
            else
            {
                if (candidateFireAt is null)
                {
                    fireAt = null;
                }
                else
                {
                    RememberPlannedTiming(
                        notificationIdentifier,
                        plannedPreparationAt,
                        lessonStartAt,
                        candidateFireAt.Value);
                    fireAt = candidateFireAt.Value;
                }
            }
        }

        if (publicationTimeChanged)
        {
            OnPublicationTimeChanged();
        }

        return fireAt;
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
    /// 仅保留本轮经系统最终确认仍存在的准备提醒；已经到达触发时间的
    /// 已确认提醒视为可能已经送达，并保留至开课。
    /// </summary>
    public void ReconcileScheduledNotifications(
        IEnumerable<string> confirmedNotificationIdentifiers,
        DateTimeOffset systemNow)
    {
        ArgumentNullException.ThrowIfNull(confirmedNotificationIdentifiers);
        var identifiers = confirmedNotificationIdentifiers.ToHashSet(StringComparer.Ordinal);
        var publicationTimeChanged = false;
        lock (_syncRoot)
        {
            RemoveExpiredTimings(systemNow);
            foreach (var identifier in _preparationTimings.Keys
                         .Where(x => !identifiers.Contains(x) &&
                                     ShouldRemoveMissingTiming(
                                         _preparationTimings[x],
                                         systemNow))
                         .ToArray())
            {
                var timing = _preparationTimings[identifier];
                publicationTimeChanged |= timing.IsScheduled ||
                                          timing.PlannedPreparationAt >
                                          systemNow + SchedulingSafetyMargin;
                _preparationTimings.Remove(identifier);
            }
        }

        if (publicationTimeChanged)
        {
            OnPublicationTimeChanged();
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
        var publicationTimeChanged = false;
        lock (_syncRoot)
        {
            if (_preparationTimings.TryGetValue(notificationIdentifier, out var timing) &&
                timing.FireAt == fireAt &&
                !timing.IsScheduled)
            {
                _preparationTimings[notificationIdentifier] = timing with
                {
                    IsScheduled = true
                };
                publicationTimeChanged = timing.FireAt != timing.PlannedPreparationAt;
            }
        }

        if (publicationTimeChanged)
        {
            OnPublicationTimeChanged();
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
        var publicationTimeChanged = false;
        lock (_syncRoot)
        {
            if (_preparationTimings.TryGetValue(notificationIdentifier, out var timing) &&
                fireAt < timing.LessonStartAt)
            {
                publicationTimeChanged = timing.FireAt != fireAt ||
                                         (!timing.IsScheduled &&
                                          timing.FireAt != timing.PlannedPreparationAt);
                _preparationTimings[notificationIdentifier] = timing with
                {
                    FireAt = fireAt,
                    IsScheduled = true
                };
            }
        }

        if (publicationTimeChanged)
        {
            OnPublicationTimeChanged();
        }
    }

    private bool RememberPlannedTiming(
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
            return false;
        }

        _preparationTimings[notificationIdentifier] = new PreparationTiming(
            plannedPreparationAt,
            lessonStartAt,
            fireAt,
            IsScheduled: false);
        return true;
    }

    private void OnPublicationTimeChanged() =>
        PublicationTimeChanged?.Invoke(this, EventArgs.Empty);

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

    private static bool ShouldRemoveMissingTiming(
        PreparationTiming timing,
        DateTimeOffset systemNow) =>
        !timing.IsScheduled ||
        timing.FireAt > systemNow + SchedulingSafetyMargin;

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
