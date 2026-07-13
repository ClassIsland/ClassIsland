using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// 遵循 iOS 生命周期约束；系统不允许应用自行重新拉起进程。
/// </summary>
internal sealed class IosAppLifetimeService(
    Func<CancellationToken, Task> prepareForManualTerminationAsync)
    : IAppLifetimeService
{
    public void Shutdown()
    {
        // iOS 应用的进程生命周期由用户和系统管理。调用方需要提示用户
        // 从 App 切换器手动结束应用，不能在这里主动终止进程。
    }

    public void Restart(string[] parameters, bool restartToLauncher)
    {
        // Apple 不允许应用主动终止后重新启动；保存一次性参数，待用户
        // 手动结束并重新打开后由 AppDelegate 消费。
        IosPendingLaunchArgumentsStore.Save(parameters);
    }

    public Task PrepareForManualTerminationAsync(
        CancellationToken cancellationToken = default)
    {
        return prepareForManualTerminationAsync(cancellationToken);
    }
}
