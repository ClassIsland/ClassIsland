using System.Runtime.Versioning;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Avalonia.Controls;
using Avalonia.Media;
using ClassIsland.Android.Services.Platform;
using ClassIsland.Extensions;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Services;

namespace ClassIsland.Android;

[Application]
[SupportedOSPlatform("android24.0")]
public class Application : AvaloniaAndroidApplication<App>
{
    
    public static Application Instance { get; private set; } = null!;
    
    protected Application(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
        Instance = this;
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        PlatformServices.AppLifetimeService = new AndroidAppLifetimeService();
        PlatformServices.LauncherService = new LauncherService();

        var restartParameters = MainActivity.Current?.TryGetTarget(out var mainActivity) == true
            ? mainActivity.Intent?.GetStringArrayExtra(AndroidAppLifetimeService.RestartParametersExtra)
            : null;
        var buildApp = Program.AppEntry(restartParameters is null
            ? ["--mobile"]
            : [.. restartParameters, "--mobile"]);

        return AppBuilder.Configure<App>(() =>
            {
                var app = buildApp();
                app.OperatingSystem = "android";
                return app;
            })
            .UseAndroid()
            .With(new FontManagerOptions
            {
                DefaultFamilyName = MainWindow.DefaultFontFamilyKey,
                FontFallbacks =
                [
                    new FontFallback
                    {
                        FontFamily = MainWindow.DefaultFontFamily
                    }
                ]
            })
            .With(new AndroidPlatformOptions()
            {
                RenderingMode = [
                    AndroidRenderingMode.Egl,
                    AndroidRenderingMode.Vulkan,
                    AndroidRenderingMode.Software
                ]
            })
            .LogToHostSink();
    }
}
