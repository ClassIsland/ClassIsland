using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.iOS;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Controls.UI;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.UI;
using ClassIsland.Core.Controls.IconSources;
using ClassIsland.Extensions;
using ClassIsland.iOS.Services.LiveActivities;
using ClassIsland.iOS.Services.Notifications;
using ClassIsland.iOS.Services.Platform;
using ClassIsland.iOS.Services.UI;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Shared;
using ClassIsland.Views;
using FluentAvalonia.UI.Controls;
using Foundation;
using UserNotifications;

namespace ClassIsland.iOS;

/// <summary>
/// 将共享 ClassIsland Avalonia 应用接入 iOS/iPadOS 的单视图生命周期。
/// </summary>
[Register("AppDelegate")]
public sealed class AppDelegate : AvaloniaAppDelegate<App>
{
    private LessonsLiveActivityCoordinator? _liveActivityCoordinator;
    private IosLessonsNotificationCoordinator? _lessonsNotificationCoordinator;
    private IosSystemEventsService? _systemEventsService;
    private readonly LessonPreparationNotificationTimeline _lessonPreparationTimeline = new();
    private IActivatableLifetime? _activatableLifetime;
    private readonly IosNotificationAuthorizationService _notificationAuthorizationService = new();
    private readonly IosNotificationCenterDelegate _notificationCenterDelegate = new();
    private App? _app;
    private Uri? _pendingNavigationUri;
    private bool _isAppNavigationReady;

    protected override AppBuilder CreateAppBuilder()
    {
        if (!IosSoundFlowNativeBootstrap.TryInitialize(out var soundFlowException))
        {
            Console.Error.WriteLine(
                $"SoundFlow iOS native bootstrap failed; audio will be unavailable: {soundFlowException}");
        }

        UNUserNotificationCenter.Current.Delegate = _notificationCenterDelegate;
        PlatformServices.AppLifetimeService = new IosAppLifetimeService(
            PrepareForManualTerminationAsync,
            ResumeAfterManualTerminationCanceled);
        PlatformServices.FilePickerService = new IosPlatformFilePickerService();
        PlatformServices.LauncherService = new IosLauncherService();
        PlatformServices.LiveActivityService = new IosLiveActivityService();
        _systemEventsService = new IosSystemEventsService();
        PlatformServices.SystemEventsService = _systemEventsService;

        var launchArguments = new List<string> { "--mobile" };
        launchArguments.AddRange(IosPendingLaunchArgumentsStore.Consume());
        var buildApp = Program.AppEntry(launchArguments.ToArray());
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
            _app = app;
            app.AppStarted += OnAppStarted;
            _activatableLifetime = app.TryGetFeature<IActivatableLifetime>();
            if (_activatableLifetime != null)
            {
                _activatableLifetime.Activated += OnActivated;
            }

            var splash = new SplashView();
            splash.Show();

            _liveActivityCoordinator = new LessonsLiveActivityCoordinator(
                PlatformServices.LiveActivityService,
                _lessonPreparationTimeline);
            _liveActivityCoordinator.Start();

            _lessonsNotificationCoordinator = new IosLessonsNotificationCoordinator(
                _notificationAuthorizationService,
                _lessonPreparationTimeline);
            _lessonsNotificationCoordinator.Start();

#if DEVELOPER_PREVIEW
            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    await ShowDeveloperPreviewWarningAsync(viewHost);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"无法显示 Developer Preview 警告：{exception}");
                }
                finally
                {
                    app.Init();
                }
            });
#else
            Dispatcher.UIThread.Post(app.Init);
#endif
        });
    }

    private void OnActivated(object? sender, ActivatedEventArgs args)
    {
        if (args is not ProtocolActivatedEventArgs protocolArguments ||
            !AppNavigationUriParser.TryParseClassIslandUri(
                protocolArguments.Uri.AbsoluteUri,
                out var uri))
        {
            return;
        }

        if (!_isAppNavigationReady)
        {
            _pendingNavigationUri = uri;
            return;
        }

        QueueNavigation(uri!);
    }

    private void OnAppStarted(object? sender, EventArgs e)
    {
        _isAppNavigationReady = true;
        if (_pendingNavigationUri is not { } uri)
        {
            return;
        }

        _pendingNavigationUri = null;
        QueueNavigation(uri);
    }

    private static void QueueNavigation(Uri uri)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (IAppHost.TryGetService<IUriNavigationService>() is { } navigationService)
            {
                navigationService.NavigateWrapped(uri);
            }
        });
    }

    private Task PrepareForManualTerminationAsync(
        CancellationToken cancellationToken)
    {
        return _liveActivityCoordinator?.EndCurrentAsync(cancellationToken)
               ?? Task.CompletedTask;
    }

    private void ResumeAfterManualTerminationCanceled()
    {
        _liveActivityCoordinator?.ResumeAfterManualTerminationCanceled();
    }

#if DEVELOPER_PREVIEW
    private static async Task ShowDeveloperPreviewWarningAsync(Control root)
    {
        var topLevel = await GetTopLevelAsync(root);
        await new FATaskDialog()
        {
            Title = "ClassIsland",
            Header = "欢迎使用 2.2-Misha Developer Preview",
            Content = "此版本仅供开发人员进行早期预览，稳定性欠佳，不适用于生产环境或日常使用。如果您在使用的过程中遇到问题，欢迎前往 GitHub issues 上提交 issue！",
            IconSource = new AdvancedImageIconSource()
            {
                Uri = "avares://ClassIsland.iOS/Assets/HoYoStickers/米沙_欢迎光临.png"
            },
            XamlRoot = topLevel,
            Buttons =
            [
                new FATaskDialogButton("确定", true)
                {
                    IsDefault = true
                }
            ]
        }.ShowAsync();
    }

    private static async Task<TopLevel> GetTopLevelAsync(Control control)
    {
        var topLevel = TopLevel.GetTopLevel(control);
        if (topLevel != null)
        {
            return topLevel;
        }

        var completionSource = new TaskCompletionSource<TopLevel>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        void OnLoaded(object? sender, RoutedEventArgs args)
        {
            var loadedTopLevel = TopLevel.GetTopLevel(control);
            if (loadedTopLevel == null)
            {
                return;
            }

            control.Loaded -= OnLoaded;
            completionSource.TrySetResult(loadedTopLevel);
        }

        control.Loaded += OnLoaded;
        topLevel = TopLevel.GetTopLevel(control);
        if (topLevel != null)
        {
            control.Loaded -= OnLoaded;
            return topLevel;
        }

        return await completionSource.Task;
    }
#endif

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_activatableLifetime != null)
            {
                _activatableLifetime.Activated -= OnActivated;
                _activatableLifetime = null;
            }

            if (_app != null)
            {
                _app.AppStarted -= OnAppStarted;
                _app = null;
            }

            if (ReferenceEquals(
                    UNUserNotificationCenter.Current.Delegate,
                    _notificationCenterDelegate))
            {
                UNUserNotificationCenter.Current.Delegate = null;
            }

            _lessonsNotificationCoordinator?.Dispose();
            _lessonsNotificationCoordinator = null;
            _liveActivityCoordinator?.Dispose();
            _liveActivityCoordinator = null;
            _systemEventsService?.Dispose();
            _systemEventsService = null;
        }

        base.Dispose(disposing);
    }
}
