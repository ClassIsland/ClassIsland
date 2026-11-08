using System.Diagnostics.CodeAnalysis;
using Foundation;
using UIKit;

namespace ClassIsland.iOS.Services.SystemIntegration;

/// <summary>
/// 保留 Avalonia 的窗口创建和 URL 激活，只补充系统搜索活动。
/// </summary>
[Register("ClassIslandSystemSceneDelegate")]
internal sealed class IosSystemSceneDelegate : UIResponder, IUIWindowSceneDelegate
{
    private readonly IUIWindowSceneDelegate _avaloniaDelegate = CreateAvaloniaDelegate();

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors |
                       DynamicallyAccessedMemberTypes.NonPublicConstructors,
        "Avalonia.iOS.AvaloniaSceneDelegate", "Avalonia.iOS")]
    private static IUIWindowSceneDelegate CreateAvaloniaDelegate() =>
        (IUIWindowSceneDelegate)Activator.CreateInstance(
            Type.GetType("Avalonia.iOS.AvaloniaSceneDelegate, Avalonia.iOS", throwOnError: true)!)!;

    [Export("window")]
    public UIWindow? Window { get; set; }

    [Export("scene:willConnectToSession:options:")]
    public void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions options)
    {
        _avaloniaDelegate.WillConnect(scene, session, options);
        Window = _avaloniaDelegate.GetWindow();
        if (UIApplication.SharedApplication.Delegate is not AppDelegate appDelegate) return;
        foreach (var activity in options.UserActivities) appDelegate.HandleSystemActivity(activity);
    }

    [Export("scene:continueUserActivity:")]
    public void ContinueUserActivity(UIScene scene, NSUserActivity activity)
    {
        if (UIApplication.SharedApplication.Delegate is AppDelegate appDelegate &&
            appDelegate.HandleSystemActivity(activity)) return;
        _avaloniaDelegate.ContinueUserActivity(scene, activity);
    }

    [Export("scene:openURLContexts:")]
    public void OpenUrlContexts(UIScene scene, NSSet<UIOpenUrlContext> contexts) =>
        _avaloniaDelegate.OpenUrlContexts(scene, contexts);

    protected override void Dispose(bool disposing)
    {
        if (disposing) _avaloniaDelegate.Dispose();
        base.Dispose(disposing);
    }
}
