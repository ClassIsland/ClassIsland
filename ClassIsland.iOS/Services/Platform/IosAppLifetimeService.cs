using ClassIsland.Platforms.Abstraction.Services;
using UIKit;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// 遵循 iOS 生命周期约束；系统不允许应用自行重新拉起进程。
/// </summary>
internal sealed class IosAppLifetimeService : IAppLifetimeService
{
    public void Shutdown()
    {
        UIApplication.SharedApplication.BeginInvokeOnMainThread(() =>
        {
            foreach (var scene in UIApplication.SharedApplication.ConnectedScenes.OfType<UIWindowScene>())
            {
                UIApplication.SharedApplication.RequestSceneSessionDestruction(
                    scene.Session,
                    null,
                    _ => { });
            }
        });
    }

    public void Restart(string[] parameters, bool restartToLauncher)
    {
        // Apple 不允许应用主动终止后重新启动；调用方会在调用前拦截此操作。
    }
}
