using System.Runtime.Versioning;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Avalonia.Controls;
using Avalonia.Media;
using ClassIsland.Android.Services.Platform;
using ClassIsland.Core;
using ClassIsland.Core.Services;
using ClassIsland.Extensions;
using ClassIsland.Helpers;
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
        PlatformServices.DesktopToastService = new DesktopToastService();
        PlatformServices.LocationService = new LocationService();
        PlatformServices.HapticFeedbackService = new HapticFeedbackService();

        var restartParameters = MainActivity.Current?.TryGetTarget(out var mainActivity) == true
            ? mainActivity.Intent?.GetStringArrayExtra(AndroidAppLifetimeService.RestartParametersExtra)
            : null;
        var buildApp = Program.AppEntry(restartParameters is null
            ? ["--mobile"]
            : [.. restartParameters, "--mobile"]);
        var renderingMode = int.TryParse(GlobalStorageService.GetValue("AndroidRenderingMode"), out var value)
            ? value
            : 0;

        return AppBuilder.Configure<App>(() =>
            {
                var app = buildApp();
                app.OperatingSystem = "android";
                return app;
            })
            .UseAndroid()
            .UseOverlayAssetLoader()
            .With(new SkiaOptions
            {
                MaxGpuResourceSizeBytes = SkiaGpuCacheHelper.SizeBytes
            })
            .With(new FontManagerOptions
            {
                FontFallbacks =
                [
                    new FontFallback
                    {
                        FontFamily = AppBase.FluentIconsFontFamily
                    }
                ]
            })
            .With(new AndroidPlatformOptions()
            {
                RenderingMode = BuildRenderingMode(renderingMode)
            })
            .LogToHostSink();
    }

    private static IReadOnlyList<AndroidRenderingMode> BuildRenderingMode(int userValue) => userValue switch
    {
        2 =>
        [
            AndroidRenderingMode.Vulkan,
            AndroidRenderingMode.Egl,
            AndroidRenderingMode.Software
        ],
        3 => [AndroidRenderingMode.Software],
        _ =>
        [
            AndroidRenderingMode.Egl,
            AndroidRenderingMode.Vulkan,
            AndroidRenderingMode.Software
        ]
    };
}
