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
        ActivatePlatforms(out var postInit);
        var buildApp = await ClassIsland.Program.AppEntry(args);
        return AppBuilder.Configure<App>(() =>
            {
                var app = buildApp();
                app.Initialized += (_, _) => postInit();
                return app;
            })
            .UsePlatformDetect()
            .LogToHostSink()
            .StartWithClassicDesktopLifetime(args);
    }
    
    // AppBuilder for designer
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
#if Platforms_Windows
        // .UseDirect2D1()
#endif
        .UsePlatformDetect()
        .LogToHostSink();

    static void ActivatePlatforms(out Action postInitCallback)
    {
        postInitCallback = () => { };
#if Platforms_Windows
        PlatformServices.WindowPlatformService = new WindowPlatformService();
        PlatformServices.LocationService = new LocationService();
#endif
#if Platforms_Linux
        var windowPlatformService = new WindowPlatformService();
        PlatformServices.WindowPlatformService = windowPlatformService;
        postInitCallback = () =>
        {
            windowPlatformService.PostInit();
        };
#endif
    }
}