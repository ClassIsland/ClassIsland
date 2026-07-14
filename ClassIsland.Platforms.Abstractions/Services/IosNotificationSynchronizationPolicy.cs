namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// iOS 本地通知同步前可独立验证的标识符计划。
/// </summary>
internal static class IosNotificationSynchronizationPolicy
{
    public static IosNotificationSynchronizationPlan CreatePlan(
        IEnumerable<string> requestedIdentifiers,
        IEnumerable<string> identifiersToUpsert,
        IEnumerable<string> pendingIdentifiers,
        string managedIdentifierPrefix,
        int maximumPendingCount)
    {
        ArgumentNullException.ThrowIfNull(requestedIdentifiers);
        ArgumentNullException.ThrowIfNull(identifiersToUpsert);
        ArgumentNullException.ThrowIfNull(pendingIdentifiers);
        ArgumentException.ThrowIfNullOrEmpty(managedIdentifierPrefix);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumPendingCount);

        var requested = requestedIdentifiers
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (requested.Any(x => !x.StartsWith(
                managedIdentifierPrefix,
                StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "Requested identifiers must belong to the managed prefix.",
                nameof(requestedIdentifiers));
        }

        var pending = pendingIdentifiers
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var pendingSet = pending.ToHashSet(StringComparer.Ordinal);
        var requestedSet = requested.ToHashSet(StringComparer.Ordinal);
        var upsert = identifiersToUpsert
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (upsert.Any(x => !requestedSet.Contains(x)))
        {
            throw new ArgumentException(
                "Identifiers to upsert must be part of the requested schedule.",
                nameof(identifiersToUpsert));
        }

        var upsertSet = upsert.ToHashSet(StringComparer.Ordinal);
        if (requested.Any(x => !pendingSet.Contains(x) && !upsertSet.Contains(x)))
        {
            throw new ArgumentException(
                "Every requested identifier that is not already pending must be upserted.",
                nameof(identifiersToUpsert));
        }

        var nonManagedPendingCount = pending.Count(x => !x.StartsWith(
            managedIdentifierPrefix,
            StringComparison.Ordinal));
        if (nonManagedPendingCount + requested.Length > maximumPendingCount)
        {
            throw new InvalidOperationException(
                "The requested managed notifications exceed the remaining native capacity.");
        }

        var obsolete = pending
            .Where(x => x.StartsWith(managedIdentifierPrefix, StringComparison.Ordinal) &&
                        !requestedSet.Contains(x))
            .OrderByDescending(x => x, StringComparer.Ordinal)
            .ToArray();
        var availableSlots = Math.Max(0, maximumPendingCount - pending.Length);
        var obsoleteIndex = 0;
        var upsertSteps = new List<IosNotificationSynchronizationStep>(upsert.Length);
        foreach (var identifier in upsert)
        {
            string? obsoleteIdentifierToRemove = null;
            if (!pendingSet.Contains(identifier))
            {
                if (availableSlots > 0)
                {
                    availableSlots--;
                }
                else
                {
                    if (obsoleteIndex >= obsolete.Length)
                    {
                        throw new InvalidOperationException(
                            "The native notification schedule cannot free enough managed slots.");
                    }

                    obsoleteIdentifierToRemove = obsolete[obsoleteIndex++];
                }
            }

            upsertSteps.Add(new IosNotificationSynchronizationStep(
                identifier,
                obsoleteIdentifierToRemove));
        }

        return new IosNotificationSynchronizationPlan(
            requested,
            upsertSteps,
            obsolete.Skip(obsoleteIndex).ToArray());
    }

    public static IReadOnlyList<string> GetMissingIdentifiers(
        IEnumerable<string> expectedIdentifiers,
        IEnumerable<string> confirmedIdentifiers)
    {
        ArgumentNullException.ThrowIfNull(expectedIdentifiers);
        ArgumentNullException.ThrowIfNull(confirmedIdentifiers);

        var confirmed = confirmedIdentifiers.ToHashSet(StringComparer.Ordinal);
        return expectedIdentifiers
            .Distinct(StringComparer.Ordinal)
            .Where(x => !confirmed.Contains(x))
            .ToArray();
    }

    public static IosNotificationRollbackPlan CreateRollbackPlan(
        IEnumerable<string> successfullyUpsertedIdentifiers,
        IEnumerable<string> originallyPendingIdentifiers,
        IEnumerable<string> removedObsoleteIdentifiers)
    {
        ArgumentNullException.ThrowIfNull(successfullyUpsertedIdentifiers);
        ArgumentNullException.ThrowIfNull(originallyPendingIdentifiers);
        ArgumentNullException.ThrowIfNull(removedObsoleteIdentifiers);

        var upserted = successfullyUpsertedIdentifiers
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var originallyPending = originallyPendingIdentifiers
            .ToHashSet(StringComparer.Ordinal);
        return new IosNotificationRollbackPlan(
            upserted.Where(x => !originallyPending.Contains(x)).ToArray(),
            upserted.Where(originallyPending.Contains).ToArray(),
            removedObsoleteIdentifiers
                .Distinct(StringComparer.Ordinal)
                .Where(originallyPending.Contains)
                .ToArray());
    }
}

internal sealed record IosNotificationSynchronizationPlan(
    IReadOnlyList<string> RequestedIdentifiers,
    IReadOnlyList<IosNotificationSynchronizationStep> UpsertSteps,
    IReadOnlyList<string> ObsoleteIdentifiersToRemoveAfterUpsert);

internal sealed record IosNotificationSynchronizationStep(
    string Identifier,
    string? ObsoleteIdentifierToRemoveBeforeUpsert);

internal sealed record IosNotificationRollbackPlan(
    IReadOnlyList<string> AddedIdentifiersToRemove,
    IReadOnlyList<string> ReplacedIdentifiersToRestore,
    IReadOnlyList<string> RemovedObsoleteIdentifiersToRestore);
