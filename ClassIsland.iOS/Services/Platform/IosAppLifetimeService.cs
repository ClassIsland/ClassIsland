using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// 遵循 iOS 生命周期约束；系统不允许应用自行重新拉起进程。
/// </summary>
internal sealed class IosAppLifetimeService(
    Func<CancellationToken, Task> prepareForManualTerminationAsync,
    Action resumeAfterManualTerminationCanceled)
    : IAppLifetimeService
{
    public void Shutdown()
    {
        // 调用方已完成数据保存和实时活动清理，再结束进程。
        Environment.Exit(0);
    }

    public void Restart(string[] parameters, bool restartToLauncher)
    {
        // iOS 没有重新拉起自身进程的公开接口；保存一次性参数，
        // 待用户重新打开后由 AppDelegate 消费。
        IosPendingLaunchArgumentsStore.Save(parameters);
    }

    public Task PrepareForManualTerminationAsync(
        CancellationToken cancellationToken = default)
    {
        return prepareForManualTerminationAsync(cancellationToken);
    }

    public void ResumeAfterManualTerminationCanceled()
    {
        resumeAfterManualTerminationCanceled();
    }
}
