using System.Reflection;
using System.Runtime.InteropServices;
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
using ClassIsland.iOS.Controls;
using ClassIsland.iOS.Services.LiveActivities;
using ClassIsland.iOS.Services.Notifications;
using ClassIsland.iOS.Services.Overlay;
using ClassIsland.iOS.Services.Platform;
using ClassIsland.iOS.Services.UI;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Shared;
using ClassIsland.Views;
using FluentAvalonia.UI.Controls;
using Foundation;
using UIKit;
using UserNotifications;

namespace ClassIsland.iOS;

/// <summary>
/// 将共享 ClassIsland Avalonia 应用接入 iOS/iPadOS 的单视图生命周期。
/// 当进程以 <c>--hud</c> 启动时，改为承载系统级悬浮主界面宿主。
/// </summary>
[Register("AppDelegate")]
public sealed class AppDelegate : AvaloniaAppDelegate<App>
{
    private readonly bool _isHudProcess = IosHudMode.IsHudProcess;
    private LessonsLiveActivityCoordinator? _liveActivityCoordinator;
    private IosLessonsNotificationCoordinator? _lessonsNotificationCoordinator;
    private IosSystemEventsService? _systemEventsService;
    private readonly LessonPreparationNotificationTimeline _lessonPreparationTimeline = new();
    private IActivatableLifetime? _activatableLifetime;
    private readonly IosNotificationAuthorizationService _notificationAuthorizationService = new();
    private readonly IosNotificationCenterDelegate _notificationCenterDelegate = new();
    private IosLocationService? _locationService;
    private IosHudOverlayService? _hudOverlayService;
    private IosHudHost? _hudHost;
    private UIWindow? _hudWindow;
    private bool _hasLoggedHudWindowFound;
    private DispatcherTimer? _hudWindowRegistrationTimer;
    private DispatcherTimer? _hudWatchdogTimer;
    private int _hudWindowRegistrationAttempts;
    private App? _app;
    private Uri? _pendingNavigationUri;
    private bool _isAppNavigationReady;

    protected override AppBuilder CreateAppBuilder()
    {
        IosStartupDiagnostics.Write(
            _isHudProcess ? "Creating Avalonia app builder (HUD)" : "Creating Avalonia app builder");
        if (!IosSoundFlowNativeBootstrap.TryInitialize(out var soundFlowException))
        {
            Console.Error.WriteLine(
                $"SoundFlow iOS native bootstrap failed; audio will be unavailable: {soundFlowException}");
        }

        PlatformServices.AppLifetimeService = new IosAppLifetimeService(
            PrepareForManualTerminationAsync,
            ResumeAfterManualTerminationCanceled);
        PlatformServices.FilePickerService = new IosPlatformFilePickerService();
        PlatformServices.LauncherService = new IosLauncherService();

        if (!_isHudProcess)
        {
            UNUserNotificationCenter.Current.Delegate = _notificationCenterDelegate;
            _locationService = new IosLocationService();
            PlatformServices.LocationService = _locationService;
            PlatformServices.LiveActivityService = new IosLiveActivityService();
            _systemEventsService = new IosSystemEventsService();
            PlatformServices.SystemEventsService = _systemEventsService;
#if CLASSISLAND_TROLLSTORE
            _hudOverlayService = new IosHudOverlayService();
            PlatformServices.MainWindowOverlayService = _hudOverlayService;
#endif
        }

        var launchArguments = new List<string> { "--mobile" };
        if (_isHudProcess)
        {
            launchArguments.Add("--hud");
        }
        else
        {
            launchArguments.AddRange(IosPendingLaunchArgumentsStore.Consume());
        }

        IosStartupDiagnostics.Write("Starting shared AppEntry");
        var buildApp = Program.AppEntry(launchArguments.ToArray());
        if (_isHudProcess && !App.ApplicationCommand.Hud)
        {
            // 共享入口的启动参数解析若丢失 --hud（例如上游合并覆盖），HUD 子进程会误走普通移动端
            // 启动路径并卡在 MainView 加载。这里兜底，保证 HUD 进程一定使用只读快照与启动捷径。
            IosStartupDiagnostics.Write("HUD mode: 共享入口未识别 --hud，已回退为 HUD 模式");
            App.ApplicationCommand.Hud = true;
        }
        IosStartupDiagnostics.Write($"Shared AppEntry completed (hud={App.ApplicationCommand.Hud})");
        return AppBuilder.Configure<App>(() =>
            {
                IosStartupDiagnostics.Write("Constructing shared App");
                var app = buildApp();
                app.OperatingSystem = "ios";
                IosStartupDiagnostics.Write("Shared App constructed");
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
            IosStartupDiagnostics.Write("Avalonia setup completed; creating mobile host");
            if (appBuilder.Instance is not App app ||
                app.ApplicationLifetime is not ISingleViewApplicationLifetime lifetime)
            {
                throw new InvalidOperationException("iOS Avalonia 单视图生命周期尚未初始化。");
            }

            _app = app;
            app.AppStarted += OnAppStarted;

            if (_isHudProcess)
            {
                SetupHudHost(app, lifetime);
                return;
            }

            var viewHost = new MobileViewHost();
            IViewHostProvider.Instance = new IosViewHostProvider(viewHost);
            lifetime.MainView = viewHost;
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

            Dispatcher.UIThread.Post(async () =>
            {
                // 启动检查可能立即显示对话框，必须先准备好共享根窗口。
                app.PhonyRootWindow = await GetTopLevelAsync(viewHost);
                IosStartupDiagnostics.Write("Mobile host ready; starting App.Init");
#if DEVELOPER_PREVIEW
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
#else
                app.Init();
#endif
            });
        });
    }

    private void SetupHudHost(App app, ISingleViewApplicationLifetime lifetime)
    {
        IosStartupDiagnostics.Write("HUD mode: creating overlay host");
        _hudHost = new IosHudHost();
        // 共享代码在 HUD 模式不会导航页面，仅需要一个可用的宿主提供方以满足初始化路径。
        IViewHostProvider.Instance = new IosViewHostProvider(new MobileViewHost());
        lifetime.MainView = _hudHost;
        Dispatcher.UIThread.Post(async () =>
        {
            // HUD 子进程不会被 SpringBoard 分配 UIScene，Avalonia 12 的 iOS 后端不会自动创建窗口。
            EnsureHudWindow(lifetime);
            app.PhonyRootWindow = await GetTopLevelAsync(_hudHost);
            IosStartupDiagnostics.Write("HUD host ready; starting App.Init");
            app.Init();
        });
    }

    /// <summary>
    /// 在 HUD 子进程里手动创建 Avalonia 单视图窗口。
    /// Avalonia 12 的 iOS 后端只在 UIScene 连接时创建 UIWindow；HUD 进程由主 App 通过
    /// posix_spawn 拉起，永远等不到场景回调，若不补这一步，GetTopLevelAsync 会永久等待 Loaded。
    /// </summary>
    private void EnsureHudWindow(ISingleViewApplicationLifetime lifetime)
    {
        if (_hudWindow != null || TopLevel.GetTopLevel(_hudHost!) != null)
        {
            return;
        }

        try
        {
            var sceneDelegateType = typeof(AvaloniaView).Assembly.GetType("Avalonia.iOS.AvaloniaSceneDelegate");
            var initWindow = sceneDelegateType?.GetMethod(
                "InitWindow",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (initWindow == null)
            {
                IosStartupDiagnostics.Write("HUD mode: 找不到 AvaloniaSceneDelegate.InitWindow，无法创建悬浮窗");
                return;
            }

            var window = new UIWindow(UIScreen.MainScreen.Bounds)
            {
                BackgroundColor = UIColor.Clear
            };
            initWindow.Invoke(null, [window, lifetime]);
            window.MakeKeyAndVisible();
            _hudWindow = window;
            if (_hudHost != null)
            {
                _hudHost.HostWindow = window;
            }
            IosStartupDiagnostics.Write($"HUD mode: 无场景，已手动创建悬浮窗 (bounds={window.Bounds})");
        }
        catch (Exception exception)
        {
            IosStartupDiagnostics.Write("HUD mode: 手动创建悬浮窗失败", exception);
        }
    }

    private void OnActivated(object? sender, ActivatedEventArgs args)
    {
        if (_isHudProcess)
        {
            return;
        }

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
        IosStartupDiagnostics.Write("App startup completed");
        if (_isHudProcess)
        {
            IosStartupDiagnostics.Write("HUD mode: app started, initializing island");
            Dispatcher.UIThread.Post(() =>
            {
                _hudHost?.InitializeIsland();
                StartHudWindowRegistration();
                StartHudWatchdog();
            }, DispatcherPriority.Loaded);
            return;
        }

        _locationService?.SetApplicationReady();
        _hudOverlayService?.ResumeIfEnabled();
        _isAppNavigationReady = true;
        if (_pendingNavigationUri is not { } uri)
        {
            return;
        }

        _pendingNavigationUri = null;
        QueueNavigation(uri);
    }

    private void StartHudWindowRegistration()
    {
        _hudWindowRegistrationAttempts = 0;
        _hudWindowRegistrationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _hudWindowRegistrationTimer.Tick += (_, _) => TryRegisterHudWindow();
        _hudWindowRegistrationTimer.Start();
        TryRegisterHudWindow();
    }

    private void TryRegisterHudWindow()
    {
        if (_hudWindowRegistrationAttempts++ > 100)
        {
            _hudWindowRegistrationTimer?.Stop();
            IosStartupDiagnostics.Write("HUD window was not created in time; overlay registration aborted");
            return;
        }

        // HUD 子进程没有 UIScene，手动创建的窗口不在 UIApplication.Windows 中，优先用注入的引用。
        var window = _hudWindow ?? UIApplication.SharedApplication.Windows.FirstOrDefault();
        if (window == null)
        {
            return;
        }

        _hudWindow = window;
        if (_hudHost != null)
        {
            _hudHost.HostWindow = window;
        }

        if (!_hasLoggedHudWindowFound)
        {
            _hasLoggedHudWindowFound = true;
            IosStartupDiagnostics.Write(
                $"HUD window found: bounds={window.Bounds}, level={window.WindowLevel}, hidden={window.Hidden}");
        }

        window.BackgroundColor = UIColor.Clear;
        IosHudNativeBridge.RegisterWindow(window.Handle);
        var registrationError = IosHudNativeBridge.GetLastError();
        if (!string.IsNullOrEmpty(registrationError))
        {
            // 窗口可能尚未上屏（没有 CAContext），保留定时器继续重试。
            IosStartupDiagnostics.Write(
                $"HUD window registration attempt {_hudWindowRegistrationAttempts} failed: {registrationError}");
            return;
        }

        _hudWindowRegistrationTimer?.Stop();
        _hudWindowRegistrationTimer = null;
        IosStartupDiagnostics.Write("HUD window registered to SpringBoard");
    }

    private void StartHudWatchdog()
    {
        _hudWatchdogTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _hudWatchdogTimer.Tick += (_, _) =>
        {
            try
            {
                // HUD 进程是主进程 spawn 出来的；主进程退出后由 launchd 收养，此时应自行退出避免孤儿悬浮窗。
                if (getppid() == 1)
                {
                    IosStartupDiagnostics.Write("HUD parent process exited; terminating HUD process");
                    _hudWatchdogTimer?.Stop();
                    Environment.Exit(0);
                }
            }
            catch (Exception exception)
            {
                IosStartupDiagnostics.Write("HUD watchdog failed", exception);
            }
        };
        _hudWatchdogTimer.Start();
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

#endif

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

    [DllImport("libc", EntryPoint = "getppid")]
    private static extern int getppid();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hudWindowRegistrationTimer?.Stop();
            _hudWindowRegistrationTimer = null;
            _hudWatchdogTimer?.Stop();
            _hudWatchdogTimer = null;

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

            _hudOverlayService?.Shutdown();
            _hudOverlayService = null;
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
