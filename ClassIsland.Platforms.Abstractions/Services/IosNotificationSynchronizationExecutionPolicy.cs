namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// 决定通知同步是否应延后或短期重试的纯策略。
/// </summary>
internal static class IosNotificationSynchronizationExecutionPolicy
{
    public static int GetMutationCount(IosNotificationSynchronizationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return plan.UpsertSteps.Count +
               plan.UpsertSteps.Count(x =>
                   x.ObsoleteIdentifierToRemoveBeforeUpsert != null) +
               (plan.ObsoleteIdentifiersToRemoveAfterUpsert.Count > 0 ? 1 : 0);
    }

    public static bool ShouldDeferLargeMutation(
        int mutationCount,
        int maximumBackgroundMutationCount,
        bool allowLargeMutations)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(mutationCount);
        ArgumentOutOfRangeException.ThrowIfNegative(maximumBackgroundMutationCount);
        return !allowLargeMutations &&
               mutationCount > maximumBackgroundMutationCount;
    }

    public static bool ShouldRetry(
        bool skippedExpiredRequest,
        int candidateCount,
        int synchronizedCount,
        bool hasTransientCapacityPressure)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(candidateCount);
        ArgumentOutOfRangeException.ThrowIfNegative(synchronizedCount);
        if (synchronizedCount > candidateCount)
        {
            throw new ArgumentOutOfRangeException(nameof(synchronizedCount));
        }

        return skippedExpiredRequest ||
               synchronizedCount < candidateCount &&
               hasTransientCapacityPressure;
    }
}
