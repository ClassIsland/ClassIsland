using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// 遵循 iOS 生命周期约束；系统不允许应用自行重新拉起进程。
/// </summary>
internal sealed class IosAppLifetimeService : IAppLifetimeService
{
    public void Shutdown()
    {
        // iOS 没有公开的“关闭应用”API，销毁 Scene 也不会终止当前的
        // Avalonia 单视图进程。共享层已在调用此方法前同步保存关键配置；
        // 这里按产品要求结束当前侧载进程，随后由用户手动重新打开。
        Environment.Exit(0);
    }

    public void Restart(string[] parameters, bool restartToLauncher)
    {
        // Apple 不允许应用主动终止后重新启动；共享层会改为正常停止应用，等待用户手动重新打开。
    }
}
