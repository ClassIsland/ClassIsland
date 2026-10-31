using System.Runtime.CompilerServices;
using ClassIsland.iOS.Services.Overlay;
using ClassIsland.iOS.Services.Platform;
using UIKit;

namespace ClassIsland.iOS;

public static class MainClass
{
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
            RunApplication(args);
        }
        catch (Exception exception)
        {
            IosStartupDiagnostics.Write("Application entry point failed", exception);
            throw;
        }
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
