using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.iOS;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Controls.UI;
using ClassIsland.Core.Abstractions.Services.UI;
using ClassIsland.Extensions;
using ClassIsland.iOS.Services.LiveActivities;
using ClassIsland.iOS.Services.Platform;
using ClassIsland.iOS.Services.UI;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Views;
using Foundation;

namespace ClassIsland.iOS;

/// <summary>
/// 将共享 ClassIsland Avalonia 应用接入 iOS/iPadOS 的单视图生命周期。
/// </summary>
[Register("AppDelegate")]
public sealed class AppDelegate : AvaloniaAppDelegate<App>
{
    private LessonsLiveActivityCoordinator? _liveActivityCoordinator;

    protected override AppBuilder CreateAppBuilder()
    {
        PlatformServices.AppLifetimeService = new IosAppLifetimeService();
        PlatformServices.LiveActivityService = new IosLiveActivityService();

        var buildApp = Program.AppEntry(["--mobile"]);
        return AppBuilder.Configure<App>(() =>
            {
                var app = buildApp();
                app.OperatingSystem = "ios";
                return app;
            })
            .UseiOS(this)
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
            .LogToHostSink();
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return builder.AfterSetup(appBuilder =>
        {
            if (appBuilder.Instance is not App app ||
                app.ApplicationLifetime is not ISingleViewApplicationLifetime lifetime)
            {
                throw new InvalidOperationException("iOS Avalonia 单视图生命周期尚未初始化。");
            }

            var viewHost = new MobileViewHost();
            IViewHostProvider.Instance = new IosViewHostProvider(viewHost);
            lifetime.MainView = viewHost;

            var splash = new SplashView();
            splash.Show();

            _liveActivityCoordinator = new LessonsLiveActivityCoordinator(
                PlatformServices.LiveActivityService);
            _liveActivityCoordinator.Start();

            Dispatcher.UIThread.Post(app.Init);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _liveActivityCoordinator?.Dispose();
            _liveActivityCoordinator = null;
        }

        base.Dispose(disposing);
    }
}
