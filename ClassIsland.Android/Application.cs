using System.Runtime.Versioning;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Avalonia.Controls;
using Avalonia.Media;
using ClassIsland.Android.Helpers;
using ClassIsland.Android.Services.Platform;
using ClassIsland.Core.Services;
using ClassIsland.Extensions;
using ClassIsland.Helpers;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Services;
using ClassIsland.Core.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

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
        NavigationRepeaterAutomationFix.Initialize();

        PlatformServices.AppLifetimeService = new AndroidAppLifetimeService();
        PlatformServices.LauncherService = new LauncherService();
        PlatformServices.DesktopToastService = new DesktopToastService();
        PlatformServices.LocationService = new LocationService();
        PlatformServices.HapticFeedbackService = new HapticFeedbackService();
        PlatformServices.DeviceInfoService = new DeviceInfoService();

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
                app.ConfigurePlatformServices = services =>
                    services.AddSingleton<IAccentPickerService, AndroidAccentPickerService>();
                app.OperatingSystem = "android";
                return app;
            })
            .UseAndroid()
            .UseOverlayAssetLoader()
            .With(new SkiaOptions
            {
                MaxGpuResourceSizeBytes = SkiaGpuCacheHelper.SizeBytes
            })
            .With<FontManagerOptions>(AndroidFontHelper.GetOptions)
            .AfterPlatformServicesSetup(_ => AndroidFontHelper.InitializeFallbacks())
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
