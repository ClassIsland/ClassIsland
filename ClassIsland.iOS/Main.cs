using System.Runtime.CompilerServices;
using ClassIsland.iOS.Services.Platform;
using UIKit;

namespace ClassIsland.iOS;

public static class MainClass
{
    public static void Main(string[] args)
    {
        IosStartupDiagnostics.Initialize(
            Environment.GetFolderPath(Environment.SpecialFolder.Personal));
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
