using ClassIsland.Platforms.Abstraction.Models.LiveActivities;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Abstraction.Stubs.Services;

/// <summary>
/// 不支持实时活动的平台所使用的安全降级实现。
/// </summary>
public sealed class LiveActivityServiceStub : ILiveActivityService
{
    /// <inheritdoc />
    public LiveActivityAvailability Availability => LiveActivityAvailability.Unsupported;

    /// <inheritdoc />
    public Task<LiveActivityResult> PublishAsync(
        LessonLiveActivityContent content,
        CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<LiveActivityResult>(cancellationToken)
            : Task.FromResult(new LiveActivityResult(LiveActivityResultCode.Unsupported));

    /// <inheritdoc />
    public Task<LiveActivityResult> EndAsync(
        LiveActivityDismissalPolicy dismissalPolicy = LiveActivityDismissalPolicy.Default,
        CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<LiveActivityResult>(cancellationToken)
            : Task.FromResult(new LiveActivityResult(LiveActivityResultCode.Unsupported));
}
