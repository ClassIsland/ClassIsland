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
using ClassIsland.Extensions;
using ClassIsland.Platforms.Abstraction;



namespace ClassIsland.Desktop;

class Program
{
    [STAThread]
    static async Task<int> Main(string[] args)
    {
        var stopTokenSource = new CancellationTokenSource();
        ActivatePlatforms(out var postInit, stopTokenSource.Token);
        var buildApp = await ClassIsland.Program.AppEntry(args);
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
            .UsePlatformDetect()
            .LogToHostSink();

        try
        {
            return r.StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            await File.WriteAllTextAsync(Path.Combine(CommonDirectories.AppRootFolderPath, "crash.txt"), e.ToString());
            return -1;
        }
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
        postInitCallback = () =>
        {
            service.PostInit();
        };
#endif
    }
}