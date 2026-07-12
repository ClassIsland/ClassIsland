using ClassIsland.Platforms.Abstraction.Models.LiveActivities;
using ClassIsland.Platforms.Abstraction.Stubs.Services;
using ClassIsland.Platforms.Abstraction;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class LiveActivityContractsTests
{
    [Fact]
    public void Content_HasProgress_RequiresCompleteIncreasingRange()
    {
        var start = new DateTimeOffset(2026, 7, 12, 8, 0, 0, TimeSpan.FromHours(8));
        var content = CreateContent(start, start.AddMinutes(45));

        Assert.Equal("20260712:on-class:1", content.IntervalId);
        Assert.Equal(LessonLiveActivityPhase.OnClass, content.Phase);
        Assert.Equal("上课 · 数学", content.Title);
        Assert.Equal("张老师", content.Subtitle);
        Assert.Equal("08:00–08:45", content.Detail);
        Assert.Equal("数学", content.CompactText);
        Assert.Equal("classisland://live-activity", content.DeepLink);
        Assert.True(content.HasProgress);
        Assert.False(CreateContent(start, start).HasProgress);
        Assert.False(CreateContent(start, start.AddMinutes(-1)).HasProgress);
        Assert.False(CreateContent(start, null).HasProgress);
        Assert.False(CreateContent(null, start).HasProgress);
    }

    [Fact]
    public void NativeContractEnums_HaveStableValues()
    {
        Assert.Equal(0, (int)LessonLiveActivityPhase.None);
        Assert.Equal(1, (int)LessonLiveActivityPhase.OnClass);
        Assert.Equal(2, (int)LessonLiveActivityPhase.Breaking);
        Assert.Equal(3, (int)LessonLiveActivityPhase.AfterSchool);

        Assert.Equal(0, (int)LiveActivityResultCode.Succeeded);
        Assert.Equal(4, (int)LiveActivityResultCode.NativeFailure);
        Assert.Equal(0, (int)LiveActivityDismissalPolicy.Default);
        Assert.Equal(1, (int)LiveActivityDismissalPolicy.Immediate);
    }

    [Fact]
    public void Result_IsSuccess_OnlyForSucceededCode()
    {
        var result = new LiveActivityResult(
            LiveActivityResultCode.Succeeded,
            "activity-id",
            "ignored");

        Assert.Equal("activity-id", result.ActivityId);
        Assert.Equal("ignored", result.ErrorMessage);
        Assert.True(result.IsSuccess);
        Assert.False(new LiveActivityResult(LiveActivityResultCode.Disabled).IsSuccess);
    }

    [Fact]
    public async Task Stub_ReturnsUnsupportedWithoutThrowing()
    {
        var service = new LiveActivityServiceStub();

        Assert.Equal(LiveActivityAvailability.Unsupported, service.Availability);
        Assert.Equal(
            LiveActivityResultCode.Unsupported,
            (await service.PublishAsync(CreateContent())).Code);
        Assert.Equal(
            LiveActivityResultCode.Unsupported,
            (await service.EndAsync()).Code);
    }

    [Fact]
    public void PlatformServices_DefaultsToSafeStub()
    {
        Assert.IsType<LiveActivityServiceStub>(PlatformServices.LiveActivityService);
        Assert.False(PlatformServices.IsLocationSupported);
    }

    [Fact]
    public async Task Stub_ObservesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = new LiveActivityServiceStub();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.PublishAsync(CreateContent(), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.EndAsync(cancellationToken: cancellation.Token));
    }

    private static LessonLiveActivityContent CreateContent(
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null) =>
        new(
            "20260712:on-class:1",
            LessonLiveActivityPhase.OnClass,
            "上课 · 数学",
            "张老师",
            "08:00–08:45",
            "数学",
            startTime,
            endTime);
}
