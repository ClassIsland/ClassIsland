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
                app.Initialized += (_, _) => postInit();
                app.AppStopping += (_, _) => stopTokenSource.Cancel();
                return app;
            })
            .UsePlatformDetect()
#if Platforms_Windows
            // .UseDirect2D1()  // 完全用不了，https://github.com/AvaloniaUI/Avalonia/issues/11802
#endif
            .LogToHostSink();

        try
        {
            return r.StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            await File.WriteAllTextAsync(e.ToString(), Path.Combine(CommonDirectories.AppRootFolderPath, "crash.txt"));
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