namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// iOS 本地通知中心的容量分配策略。
/// </summary>
internal static class IosNotificationCapacityPolicy
{
    internal const int MaximumPendingNotificationCount = 64;
    internal const int ReservedFallbackNotificationCount = 1;
    internal const string ImmediateFallbackIdentifierPrefix =
        "classisland.notification.";

    public static int GetMaximumManagedNotificationCount(
        int configuredMaximumManagedCount,
        int nonManagedPendingCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(configuredMaximumManagedCount);
        ArgumentOutOfRangeException.ThrowIfNegative(nonManagedPendingCount);

        return Math.Min(
            configuredMaximumManagedCount,
            Math.Max(
                0,
                MaximumPendingNotificationCount -
                nonManagedPendingCount -
                ReservedFallbackNotificationCount));
    }

    public static IosFallbackNotificationCapacityDecision GetFallbackSubmissionDecision(
        string identifier,
        IEnumerable<string> pendingIdentifiers,
        TimeSpan capacityWaitElapsed,
        TimeSpan maximumCapacityWait)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);
        ArgumentNullException.ThrowIfNull(pendingIdentifiers);
        if (capacityWaitElapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(capacityWaitElapsed));
        }
        if (maximumCapacityWait <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCapacityWait));
        }

        var pending = pendingIdentifiers.ToHashSet(StringComparer.Ordinal);
        if (pending.Contains(identifier) ||
            pending.Count < MaximumPendingNotificationCount)
        {
            return IosFallbackNotificationCapacityDecision.Submit;
        }

        return capacityWaitElapsed < maximumCapacityWait
            ? IosFallbackNotificationCapacityDecision.Retry
            : IosFallbackNotificationCapacityDecision.CapacityExhausted;
    }
}

internal enum IosFallbackNotificationCapacityDecision
{
    Submit,
    Retry,
    CapacityExhausted
}

/// <summary>
/// 容量耗尽后跨连续积压短路一段时间，避免每个 host 批次重复超时。
/// </summary>
internal sealed class IosFallbackCapacityBacklogGate
{
    private long _retryAfterUtcTicks;

    public bool CanWaitForCapacity(DateTimeOffset utcNow)
    {
        var retryAfterUtcTicks = Volatile.Read(ref _retryAfterUtcTicks);
        return retryAfterUtcTicks == 0 ||
               utcNow.UtcDateTime.Ticks >= retryAfterUtcTicks;
    }

    public void MarkCapacityAvailable() =>
        Interlocked.Exchange(ref _retryAfterUtcTicks, 0);

    public void MarkCapacityExhausted(
        DateTimeOffset utcNow,
        TimeSpan probeInterval)
    {
        if (probeInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(probeInterval));
        }

        Interlocked.Exchange(
            ref _retryAfterUtcTicks,
            utcNow.Add(probeInterval).UtcDateTime.Ticks);
    }
}
