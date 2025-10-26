#if Platforms_Windows
using ClassIsland.Platform.Windows.Services;
#endif
#if Platforms_Linux
using ClassIsland.Platforms.Linux.Services;
#endif
#if Platforms_MacOs
using ClassIsland.Platforms.MacOs.Services;
#endif
using System.Diagnostics;
using Avalonia;
using Avalonia.Logging;
using Avalonia.Media;
using ClassIsland.Core;
using ClassIsland.Core.Services;
using ClassIsland.Extensions;
using ClassIsland.Models;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Shared.Helpers;


namespace ClassIsland.Desktop;

class Program
{
    [STAThread]
    static async Task<int> Main(string[] args)
    {
        var stopTokenSource = new CancellationTokenSource();
        ActivatePlatforms(out var postInit, stopTokenSource.Token);
        var buildApp = ClassIsland.Program.AppEntry(args);
        var renderingMode = int.TryParse(GlobalStorageService.GetValue("Win32RenderingMode") ?? "", out var v1)
            ? v1
            : 0;
        
        var r =  AppBuilder.Configure<App>(() =>
            {
                var app = buildApp();
#if Platforms_Windows
                app.OperatingSystem = "windows";
#endif
#if Platforms_Linux
                app.OperatingSystem = "linux";
#endif
#if Platforms_MacOs
                app.OperatingSystem = "macos";
#endif
                app.Initialized += (_, _) => postInit();
                app.AppStopping += (_, _) => stopTokenSource.Cancel();
                return app;
            })
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
            .With(new Win32PlatformOptions()
            {
                // 禁用 DirectComposition 以修复在部分版本 Windows 上 CPU 占用过高的问题。
                // https://github.com/AvaloniaUI/Avalonia/issues/16750
                CompositionMode = [
                    Win32CompositionMode.WinUIComposition,
                    Win32CompositionMode.RedirectionSurface
                ],
                RenderingMode = BuildRenderingMode(renderingMode)
            })
#if DEBUG
            .WithDeveloperTools()
#endif
            .UsePlatformDetect()
            .LogToHostSink();

        return r.StartWithClassicDesktopLifetime(args);
    }
    
    // AppBuilder for designer
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToHostSink();

    static void ActivatePlatforms(out Action postInitCallback, CancellationToken stopToken)
    {
        postInitCallback = () => { };
#if Platforms_Windows
        PlatformServices.WindowPlatformService = new WindowPlatformService();
        PlatformServices.LocationService = new LocationService();
        PlatformServices.DesktopService = new DesktopService();
        PlatformServices.SystemEventsService = new SystemEventsService();
        PlatformServices.DesktopToastService = new DesktopToastService();
        PlatformServices.FilePickerService = new PlatformFilePickerService();
        postInitCallback = () =>
        {
            AppBase.Current.AppStarted += (sender, args) =>
            {
                
            };
        };
#endif
#if Platforms_Linux
        var windowPlatformService = new WindowPlatformService(stopToken);
        PlatformServices.WindowPlatformService = windowPlatformService;
        PlatformServices.DesktopService = new DesktopService();
        var desktopToastService = new DesktopToastService();
        PlatformServices.DesktopToastService = desktopToastService;
        postInitCallback = async void () =>
        {
            windowPlatformService.PostInit();
            await desktopToastService.InitializeAsync();
        };
#endif
#if Platforms_MacOs
        var service = new WindowPlatformServices();
        PlatformServices.WindowPlatformService = service;
        PlatformServices.LocationService = new LocationService();
        PlatformServices.DesktopToastService = new DesktopToastService();
        postInitCallback = () =>
        {
            service.PostInit();
        };
#endif
    }

    private static IReadOnlyList<Win32RenderingMode> BuildRenderingMode(int userValue)
    {
        if (userValue is <= 0 or > 4)
        {
            return
            [
                Win32RenderingMode.AngleEgl,
                Win32RenderingMode.Software
            ];
        }

        return
        [
            (Win32RenderingMode)userValue,
            Win32RenderingMode.AngleEgl,
            Win32RenderingMode.Software
        ];
    }
}