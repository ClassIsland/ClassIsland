using ClassIsland.iOS.Services.Notifications;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class IosNotificationSynchronizationPolicyTests
{
    [Fact]
    public void CreatePlan_DeduplicatesRequestsAndOnlyRemovesManagedObsoleteItems()
    {
        var plan = IosNotificationSynchronizationPolicy.CreatePlan(
            ["classisland.lessons.keep", "classisland.lessons.new", "classisland.lessons.keep"],
            ["classisland.lessons.keep", "classisland.lessons.new"],
            ["classisland.lessons.keep", "classisland.lessons.old", "other.pending"],
            "classisland.lessons.",
            64);

        Assert.Equal(
            ["classisland.lessons.keep", "classisland.lessons.new"],
            plan.RequestedIdentifiers);
        Assert.Equal(
            ["classisland.lessons.keep", "classisland.lessons.new"],
            plan.UpsertSteps.Select(x => x.Identifier));
        Assert.All(
            plan.UpsertSteps,
            x => Assert.Null(x.ObsoleteIdentifierToRemoveBeforeUpsert));
        Assert.Equal(
            ["classisland.lessons.old"],
            plan.ObsoleteIdentifiersToRemoveAfterUpsert);
    }

    [Fact]
    public void CreatePlan_FullScheduleReplacement_RotatesOneVerifiedSlotAtATime()
    {
        var requested = Enumerable.Range(0, 60)
            .Select(x => $"classisland.lessons.202608{x + 1:D2}.new")
            .ToArray();
        var pending = Enumerable.Range(0, 60)
            .Select(x => $"classisland.lessons.202607{x + 1:D2}.old")
            .Concat(Enumerable.Range(0, 4).Select(x => $"other.{x}"))
            .ToArray();

        var plan = IosNotificationSynchronizationPolicy.CreatePlan(
            requested,
            requested,
            pending,
            "classisland.lessons.",
            64);

        Assert.Equal(60, plan.UpsertSteps.Count);
        Assert.All(
            plan.UpsertSteps,
            step => Assert.NotNull(step.ObsoleteIdentifierToRemoveBeforeUpsert));
        Assert.Empty(plan.ObsoleteIdentifiersToRemoveAfterUpsert);

        var projectedPending = pending.ToHashSet(StringComparer.Ordinal);
        foreach (var step in plan.UpsertSteps)
        {
            Assert.True(projectedPending.Remove(
                step.ObsoleteIdentifierToRemoveBeforeUpsert!));
            Assert.Equal(63, projectedPending.Count);
            Assert.True(projectedPending.Add(step.Identifier));
            Assert.Equal(64, projectedPending.Count);
        }

        Assert.True(requested.All(projectedPending.Contains));
        Assert.True(Enumerable.Range(0, 4)
            .Select(x => $"other.{x}")
            .All(projectedPending.Contains));
    }

    [Fact]
    public void CreatePlan_WithSpareCapacity_DefersObsoleteRemovalUntilAfterVerification()
    {
        var requested = Enumerable.Range(0, 30)
            .Select(x => $"classisland.lessons.202608{x + 1:D2}.new")
            .ToArray();
        var pending = Enumerable.Range(0, 30)
            .Select(x => $"classisland.lessons.202607{x + 1:D2}.old")
            .Concat(Enumerable.Range(0, 4).Select(x => $"other.{x}"))
            .ToArray();

        var plan = IosNotificationSynchronizationPolicy.CreatePlan(
            requested,
            requested,
            pending,
            "classisland.lessons.",
            64);

        Assert.All(
            plan.UpsertSteps,
            x => Assert.Null(x.ObsoleteIdentifierToRemoveBeforeUpsert));
        Assert.Equal(30, plan.ObsoleteIdentifiersToRemoveAfterUpsert.Count);
    }

    [Fact]
    public void GetMissingIdentifiers_DetectsNativeSilentDrop()
    {
        var missing = IosNotificationSynchronizationPolicy.GetMissingIdentifiers(
            ["first", "silently-dropped", "first", "last"],
            ["first", "last", "unmanaged"]);

        Assert.Equal(["silently-dropped"], missing);
    }

    [Fact]
    public void CreateRollbackPlan_RemovesNewItemsAndRestoresReplacedItems()
    {
        var plan = IosNotificationSynchronizationPolicy.CreateRollbackPlan(
            ["existing", "added", "existing"],
            ["existing", "obsolete", "untouched"],
            ["obsolete"]);

        Assert.Equal(["added"], plan.AddedIdentifiersToRemove);
        Assert.Equal(["existing"], plan.ReplacedIdentifiersToRestore);
        Assert.Equal(
            ["obsolete"],
            plan.RemovedObsoleteIdentifiersToRestore);
    }

    [Fact]
    public void CreatePlan_RejectsRequestsBeyondRemainingCapacity()
    {
        var requested = Enumerable.Range(0, 60)
            .Select(x => $"classisland.lessons.{x}")
            .ToArray();

        Assert.Throws<InvalidOperationException>(() =>
            IosNotificationSynchronizationPolicy.CreatePlan(
                requested,
                requested,
                Enumerable.Range(0, 5).Select(x => $"other.{x}"),
                "classisland.lessons.",
                64));
    }

    [Fact]
    public void SynchronizationPolicy_ValidatesArguments()
    {
        Assert.Throws<ArgumentNullException>(() =>
            IosNotificationSynchronizationPolicy.CreatePlan(
                null!, [], [], "classisland.lessons.", 64));
        Assert.Throws<ArgumentNullException>(() =>
            IosNotificationSynchronizationPolicy.CreatePlan(
                [], null!, [], "classisland.lessons.", 64));
        Assert.Throws<ArgumentNullException>(() =>
            IosNotificationSynchronizationPolicy.CreatePlan(
                [], [], null!, "classisland.lessons.", 64));
        Assert.Throws<ArgumentException>(() =>
            IosNotificationSynchronizationPolicy.CreatePlan([], [], [], "", 64));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            IosNotificationSynchronizationPolicy.CreatePlan(
                [], [], [], "classisland.lessons.", 0));
        Assert.Throws<ArgumentException>(() =>
            IosNotificationSynchronizationPolicy.CreatePlan(
                ["other.request"], [], [], "classisland.lessons.", 64));
        Assert.Throws<ArgumentException>(() =>
            IosNotificationSynchronizationPolicy.CreatePlan(
                ["classisland.lessons.requested"],
                ["classisland.lessons.unrequested"],
                [],
                "classisland.lessons.",
                64));
        Assert.Throws<ArgumentException>(() =>
            IosNotificationSynchronizationPolicy.CreatePlan(
                ["classisland.lessons.requested"],
                [],
                [],
                "classisland.lessons.",
                64));
        Assert.Throws<ArgumentNullException>(() =>
            IosNotificationSynchronizationPolicy.GetMissingIdentifiers(null!, []));
        Assert.Throws<ArgumentNullException>(() =>
            IosNotificationSynchronizationPolicy.GetMissingIdentifiers([], null!));
        Assert.Throws<ArgumentNullException>(() =>
            IosNotificationSynchronizationPolicy.CreateRollbackPlan(null!, [], []));
        Assert.Throws<ArgumentNullException>(() =>
            IosNotificationSynchronizationPolicy.CreateRollbackPlan([], null!, []));
        Assert.Throws<ArgumentNullException>(() =>
            IosNotificationSynchronizationPolicy.CreateRollbackPlan([], [], null!));
    }

    [Fact]
    public void ExecutionPolicy_DoesNotRetryStableCapacityReduction()
    {
        Assert.False(IosNotificationSynchronizationExecutionPolicy.ShouldRetry(
            skippedExpiredRequest: false,
            candidateCount: 60,
            synchronizedCount: 58,
            hasTransientCapacityPressure: false));
        Assert.True(IosNotificationSynchronizationExecutionPolicy.ShouldRetry(
            skippedExpiredRequest: false,
            candidateCount: 60,
            synchronizedCount: 58,
            hasTransientCapacityPressure: true));
        Assert.True(IosNotificationSynchronizationExecutionPolicy.ShouldRetry(
            skippedExpiredRequest: true,
            candidateCount: 60,
            synchronizedCount: 60,
            hasTransientCapacityPressure: false));
    }

    [Fact]
    public void ExecutionPolicy_CountsSwapRemovalAndDefersOnlyInBackground()
    {
        var plan = new IosNotificationSynchronizationPlan(
            ["keep", "new"],
            [
                new IosNotificationSynchronizationStep("keep", null),
                new IosNotificationSynchronizationStep("new", "old")
            ],
            ["remaining-old"]);

        var mutationCount = IosNotificationSynchronizationExecutionPolicy
            .GetMutationCount(plan);

        Assert.Equal(4, mutationCount);
        Assert.True(IosNotificationSynchronizationExecutionPolicy
            .ShouldDeferLargeMutation(
                mutationCount,
                maximumBackgroundMutationCount: 3,
                allowLargeMutations: false));
        Assert.False(IosNotificationSynchronizationExecutionPolicy
            .ShouldDeferLargeMutation(
                mutationCount,
                maximumBackgroundMutationCount: 3,
                allowLargeMutations: true));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            IosNotificationSynchronizationExecutionPolicy.ShouldRetry(
                skippedExpiredRequest: false,
                candidateCount: 1,
                synchronizedCount: 2,
                hasTransientCapacityPressure: false));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            IosNotificationSynchronizationExecutionPolicy
                .ShouldDeferLargeMutation(
                    mutationCount: -1,
                    maximumBackgroundMutationCount: 3,
                    allowLargeMutations: false));
    }
}
