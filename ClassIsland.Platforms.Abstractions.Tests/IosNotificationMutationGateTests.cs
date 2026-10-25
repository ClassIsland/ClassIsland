using ClassIsland.iOS.Services.Notifications;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class IosNotificationMutationGateTests
{
    [Fact]
    public async Task ExecuteAsync_PublishesConfirmedStateBeforeNextMutation()
    {
        var gate = new IosNotificationMutationGate();
        var firstEntered = CreateCompletionSource();
        var releaseFirst = CreateCompletionSource();
        var secondEntered = CreateCompletionSource();
        var confirmedStatePublished = false;

        var first = gate.ExecuteAsync(
            async () =>
            {
                firstEntered.SetResult();
                await releaseFirst.Task;
                confirmedStatePublished = true;
                return 1;
            },
            CancellationToken.None);
        await firstEntered.Task;

        var second = gate.ExecuteAsync(
            () =>
            {
                Assert.True(confirmedStatePublished);
                secondEntered.SetResult();
                return Task.FromResult(2);
            },
            CancellationToken.None);

        Assert.False(secondEntered.Task.IsCompleted);
        releaseFirst.SetResult();

        Assert.Equal(1, await first);
        Assert.Equal(2, await second);
        Assert.True(secondEntered.Task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task ExecuteAsync_ObservesCancellationWhileWaiting()
    {
        var gate = new IosNotificationMutationGate();
        var firstEntered = CreateCompletionSource();
        var releaseFirst = CreateCompletionSource();
        var first = gate.ExecuteAsync(
            async () =>
            {
                firstEntered.SetResult();
                await releaseFirst.Task;
            },
            CancellationToken.None);
        await firstEntered.Task;

        using var cancellation = new CancellationTokenSource();
        var second = gate.ExecuteAsync(
            () => Task.CompletedTask,
            cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        releaseFirst.SetResult();
        await first;
    }

    private static TaskCompletionSource CreateCompletionSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
