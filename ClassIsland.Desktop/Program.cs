#if Platforms_Windows
using ClassIsland.Platform.Windows.Services;
#endif
#if Platforms_Linux
using ClassIsland.Platforms.Linux.Services;
#endif
using Avalonia;
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
                app.Initialized += (_, _) => postInit();
                app.AppStopping += (_, _) => stopTokenSource.Cancel();
                return app;
            })
            .UsePlatformDetect()
#if Platforms_Windows
            // .UseDirect2D1()  // 完全用不了，https://github.com/AvaloniaUI/Avalonia/issues/11802
#endif
            .LogToHostSink()
            .StartWithClassicDesktopLifetime(args);
        
        return r;
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
#endif
#if Platforms_Linux
        var windowPlatformService = new WindowPlatformService(stopToken);
        PlatformServices.WindowPlatformService = windowPlatformService;
        PlatformServices.DesktopService = new DesktopService();
        postInitCallback = () =>
        {
            windowPlatformService.PostInit();
        };
#endif
    }
}