using System.Runtime.CompilerServices;
using ClassIsland.iOS.Services.Overlay;
using ClassIsland.iOS.Services.Platform;
using UIKit;

namespace ClassIsland.iOS;

public static class MainClass
{
    // UIApplication.delegate 是 unretained 属性；HUD 子进程里我们手工创建 AppDelegate，
    // 必须由托管侧保持强引用，否则对象被回收后原生回调会失效。
    private static AppDelegate? _hudAppDelegate;

    public static void Main(string[] args)
    {
        if (args.Length >= 2 && args[0] == "--hud-stop" && int.TryParse(args[1], out var targetPid))
        {
            IosHudNativeBridge.StopTarget(targetPid);
            return;
        }

        IosHudMode.Detect(args);
        if (IosHudMode.IsHudProcess)
        {
            // 在任何托管初始化之前注册退出通知；HUD 可能卡在 Avalonia 启动阶段，
            // 此时仍必须能响应主 App 的停止请求。
            IosHudNativeBridge.InitializeChildLifecycle();
        }

        var diagnosticsDirectory = IosHudMode.IsHudProcess
            ? Environment.GetEnvironmentVariable("CLASSISLAND_HUD_DATA_DIR")
            : null;
        IosStartupDiagnostics.Initialize(
            string.IsNullOrWhiteSpace(diagnosticsDirectory)
                ? Environment.GetFolderPath(Environment.SpecialFolder.Personal)
                : diagnosticsDirectory);
        try
        {
            if (IosHudMode.IsHudProcess)
            {
                RunHudApplication(args);
            }
            else
            {
                RunApplication(args);
            }
        }
        catch (Exception exception)
        {
            IosStartupDiagnostics.Write("Application entry point failed", exception);
            throw;
        }
    }

    /// <summary>
    /// HUD 子进程使用手工 UIKit 启动：该进程由主 App posix_spawn 拉起，没有 SpringBoard
    /// 提供的 UIApplication 启动上下文；标准 UIApplicationMain 创建出来的窗口拿不到
    /// CAContext（_boundContext），无法注册为 SpringBoard 全局悬浮窗口。
    /// </summary>
    private static void RunHudApplication(string[] args)
    {
        IosStartupDiagnostics.Write("Starting HUD application via manual UIKit bootstrap");
        var appDelegate = new AppDelegate();
        _hudAppDelegate = appDelegate;
        var result = IosHudNativeBridge.RunAsPlugin(appDelegate.Handle.Handle);
        if (result == 0)
        {
            IosStartupDiagnostics.Write("HUD manual bootstrap finished");
            return;
        }

        // 预检查失败时尚未初始化 UIKit，可以安全回退到标准启动路径（至少保留旧行为与日志）。
        _hudAppDelegate = null;
        IosStartupDiagnostics.Write(
            $"HUD manual bootstrap unavailable: result={result}, {IosHudNativeBridge.GetLastError()}; " +
            "falling back to UIApplicationMain");
        RunApplication(args);
    }

    // 将 UIKit 和 AppDelegate 的类型加载留在日志初始化之后。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunApplication(string[] args)
    {
        ObjCRuntime.Runtime.MarshalManagedException += (_, eventArgs) =>
            IosStartupDiagnostics.Write("Managed exception crossing native boundary", eventArgs.Exception);
        IosStartupDiagnostics.Write("Starting UIApplication");
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
