using ClassIsland.iOS.Services.Notifications;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class IosNotificationCapacityPolicyTests
{
    [Theory]
    [InlineData(0, 60)]
    [InlineData(3, 60)]
    [InlineData(4, 59)]
    [InlineData(63, 0)]
    [InlineData(64, 0)]
    public void GetMaximumManagedNotificationCount_AlwaysReservesOneFallbackSlot(
        int nonManagedPendingCount,
        int expected)
    {
        Assert.Equal(
            expected,
            IosNotificationCapacityPolicy.GetMaximumManagedNotificationCount(
                60,
                nonManagedPendingCount));
    }

    [Fact]
    public void GetFallbackSubmissionDecision_SubmitsOnlyWithoutEviction()
    {
        var pending = Enumerable.Range(0, 64)
            .Select(x => $"pending.{x}")
            .ToArray();

        Assert.Equal(
            IosFallbackNotificationCapacityDecision.Submit,
            IosNotificationCapacityPolicy.GetFallbackSubmissionDecision(
                "new",
                pending.Take(63),
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5)));
        Assert.Equal(
            IosFallbackNotificationCapacityDecision.Submit,
            IosNotificationCapacityPolicy.GetFallbackSubmissionDecision(
                pending[0],
                pending,
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(5)));
        Assert.Equal(
            IosFallbackNotificationCapacityDecision.Retry,
            IosNotificationCapacityPolicy.GetFallbackSubmissionDecision(
                "new",
                pending,
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(5)));
        Assert.Equal(
            IosFallbackNotificationCapacityDecision.CapacityExhausted,
            IosNotificationCapacityPolicy.GetFallbackSubmissionDecision(
                "new",
                pending,
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void CapacityPolicy_ValidatesArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            IosNotificationCapacityPolicy.GetMaximumManagedNotificationCount(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            IosNotificationCapacityPolicy.GetMaximumManagedNotificationCount(60, -1));
        Assert.Throws<ArgumentException>(() =>
            IosNotificationCapacityPolicy.GetFallbackSubmissionDecision(
                "",
                [],
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5)));
        Assert.Throws<ArgumentNullException>(() =>
            IosNotificationCapacityPolicy.GetFallbackSubmissionDecision(
                "new",
                null!,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            IosNotificationCapacityPolicy.GetFallbackSubmissionDecision(
                "new",
                [],
                TimeSpan.FromTicks(-1),
                TimeSpan.FromSeconds(5)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            IosNotificationCapacityPolicy.GetFallbackSubmissionDecision(
                "new",
                [],
                TimeSpan.Zero,
                TimeSpan.Zero));
    }

    [Fact]
    public void BacklogGate_ShortCircuitsAcrossBatchesUntilProbeWindow()
    {
        var gate = new IosFallbackCapacityBacklogGate();
        var now = new DateTimeOffset(2026, 7, 14, 8, 0, 0, TimeSpan.Zero);

        Assert.True(gate.CanWaitForCapacity(now));
        gate.MarkCapacityExhausted(now, TimeSpan.FromSeconds(30));
        Assert.False(gate.CanWaitForCapacity(now.AddSeconds(29)));
        Assert.True(gate.CanWaitForCapacity(now.AddSeconds(30)));

        gate.MarkCapacityAvailable();
        Assert.True(gate.CanWaitForCapacity(now));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            gate.MarkCapacityExhausted(now, TimeSpan.Zero));
    }
}
