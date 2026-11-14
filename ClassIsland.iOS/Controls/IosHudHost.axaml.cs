using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.iOS.Services.Platform;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.Shared;
using UIKit;

namespace ClassIsland.iOS.Controls;

/// <summary>
/// HUD 宿主进程的全屏透明根视图。它只承载悬浮主界面岛，窗口本身由 AppDelegate 交给原生桥
/// 注册为 SpringBoard 全局悬浮窗口，因此这里不直接操作窗口或原生桥。
/// 悬浮岛的停靠方位与偏移复用共享的 <see cref="Settings.WindowDockingLocation"/> /
/// <see cref="Settings.WindowDockingOffsetX"/> / <see cref="Settings.WindowDockingOffsetY"/>，
/// 与 Android 系统悬浮窗保持一致。
/// 由于 HUD 窗口宽度固定为屏幕宽度，而主界面行（例如课程表组件）在桌面端是靠窗口自动加宽
/// 来完整显示的，这里会按内容自然宽度整体缩放悬浮岛，保证完整内容都能落在屏幕内。
/// </summary>
public partial class IosHudHost : UserControl
{
    private const double FitCheckIntervalSeconds = 2;
    private const double FitScaleChangeThreshold = 0.01;

    private bool _isDockedTop;
    private bool _hasLoggedMissingWindow;
    private Settings? _settings;
    private LayoutTransformControl? _islandFitHost;
    private ScaleTransform? _islandFitTransform;
    private DispatcherTimer? _fitTimer;
    private double _availableWidth;
    private double _appliedFitScale = 1;

    /// <summary>
    /// 当前承载的悬浮主界面岛，未初始化时为 <see langword="null"/>。
    /// </summary>
    public IosMainWindowIslandView? Island { get; private set; }

    /// <summary>
    /// HUD 子进程的宿主窗口。HUD 进程没有 UIScene，窗口由 AppDelegate 手动创建并注入，
    /// 无法通过 <see cref="UIApplication.Windows"/> 取得。
    /// </summary>
    public UIWindow? HostWindow { get; set; }

    /// <summary>
    /// 悬浮主界面岛是否已完成初始化。
    /// </summary>
    public bool IsIslandInitialized => Island != null;

    /// <summary>
    /// 当前为适配屏幕宽度而施加的整体缩放比例。
    /// </summary>
    public double FitScale => _appliedFitScale;

    public IosHudHost()
    {
        InitializeComponent();
        // 安全区由本类按停靠方位 + 偏移统一计算，关闭 Avalonia 的自动安全区内边距，
        // 避免两者叠加导致悬浮岛位置偏移。
        TopLevel.SetAutoSafeAreaPadding(this, false);
        Loaded += OnLoaded;
    }

    /// <summary>
    /// 在主界面随窗口停靠方位创建并挂载悬浮岛。重复调用不会重复创建。
    /// </summary>
    public void InitializeIsland()
    {
        if (Island != null)
        {
            return;
        }
        // 只在应用初始化完成后调用（AppDelegate 在 AppStarted 后调用），服务缺失说明调用时机过早。
        if (IAppHost.TryGetService<SettingsService>() is not { } settingsService)
        {
            IosStartupDiagnostics.Write("HUD island initialization skipped: settings service is unavailable");
            return;
        }

        _settings = settingsService.Settings;
        _isDockedTop = _settings.WindowDockingLocation is 0 or 1 or 2;
        Island = new IosMainWindowIslandView
        {
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        // 外面再包一层 LayoutTransformControl：当悬浮岛内容自然宽度超过屏幕宽度时，
        // 按比例整体缩小，避免课程表等宽内容被裁掉。
        _islandFitTransform = new ScaleTransform(1, 1);
        _islandFitHost = new LayoutTransformControl
        {
            Child = Island,
            LayoutTransform = _islandFitTransform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = _isDockedTop ? VerticalAlignment.Top : VerticalAlignment.Bottom
        };
        Root.Children.Add(_islandFitHost);
        UpdateSafeAreaMargin();
        StartFitTimer();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // 宿主可能先于 InitializeIsland 完成加载，这里只是补一次布局计算。
        UpdateSafeAreaMargin();
    }

    private void StartFitTimer()
    {
        _fitTimer?.Stop();
        _fitTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(FitCheckIntervalSeconds)
        };
        _fitTimer.Tick += (_, _) => UpdateFitScale();
        _fitTimer.Start();
        UpdateFitScale();
    }

    /// <summary>
    /// 测量悬浮岛内容的自然宽度；超过可用宽度时整体等比缩小，保证所有组件都能显示。
    /// </summary>
    private void UpdateFitScale()
    {
        if (Island == null || _islandFitTransform == null || _availableWidth <= 0)
        {
            return;
        }

        Island.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var naturalWidth = Island.DesiredSize.Width;
        if (double.IsNaN(naturalWidth) || double.IsInfinity(naturalWidth) || naturalWidth <= 0)
        {
            return;
        }

        var fitScale = Math.Min(1, _availableWidth / naturalWidth);
        if (Math.Abs(fitScale - _appliedFitScale) < FitScaleChangeThreshold)
        {
            return;
        }

        _appliedFitScale = fitScale;
        _islandFitTransform.ScaleX = fitScale;
        _islandFitTransform.ScaleY = fitScale;
        IosStartupDiagnostics.Write(
            $"HUD island fit scale: {fitScale:F3} (available={_availableWidth:F1}, natural={naturalWidth:F1})");
    }

    private void UpdateSafeAreaMargin()
    {
        if (Island == null || _settings == null || _islandFitHost == null)
        {
            return;
        }
        if (GetHostWindow() is not { } window)
        {
            if (!_hasLoggedMissingWindow)
            {
                _hasLoggedMissingWindow = true;
                IosStartupDiagnostics.Write("HUD island layout skipped: host window is unavailable");
            }
            return;
        }

        // Avalonia DIP 与 iOS point 在设备上等值，可以直接用作 Margin。
        // 偏移量与 Android 系统悬浮窗语义一致：OffsetX > 0 向右、OffsetY > 0 向下。
        var insets = GetSafeArea(window);
        var offsetX = _settings.WindowDockingOffsetX;
        var offsetY = _settings.WindowDockingOffsetY;
        var left = Math.Max(0, insets.Left + offsetX);
        var right = Math.Max(0, insets.Right - offsetX);
        var top = _isDockedTop ? Math.Max(0, insets.Top + offsetY) : insets.Top;
        var bottom = _isDockedTop ? insets.Bottom : Math.Max(0, insets.Bottom - offsetY);
        _islandFitHost.Margin = new Thickness(left, top, right, bottom);
        _availableWidth = Math.Max(1, window.Bounds.Width - left - right);
        IosStartupDiagnostics.Write(
            $"HUD island layout: dockTop={_isDockedTop}, offset=({offsetX},{offsetY}), " +
            $"safeArea=({insets.Left},{insets.Top},{insets.Right},{insets.Bottom}), " +
            $"margin=({left},{top},{right},{bottom}), availableWidth={_availableWidth:F1}");

        // 安全区/宽度变化后立即重新计算自适应缩放。
        UpdateFitScale();
    }

    /// <summary>
    /// 取悬浮岛的屏幕安全区。HUD 窗口没有 UIScene，UIKit 可能返回全 0，
    /// 此时依次退回 Avalonia 的 InsetsManager 与状态栏/底部指示条估算值。
    /// </summary>
    private Thickness GetSafeArea(UIWindow window)
    {
        var insets = window.SafeAreaInsets;
        if (insets.Top > 0 || insets.Bottom > 0 || insets.Left > 0 || insets.Right > 0)
        {
            return new Thickness(insets.Left, insets.Top, insets.Right, insets.Bottom);
        }

        if (TopLevel.GetTopLevel(this)?.InsetsManager?.SafeAreaPadding is { } avaloniaInsets &&
            (avaloniaInsets.Top > 0 || avaloniaInsets.Bottom > 0 ||
             avaloniaInsets.Left > 0 || avaloniaInsets.Right > 0))
        {
            return avaloniaInsets;
        }

#pragma warning disable CA1422
        var statusBarHeight = UIApplication.SharedApplication.StatusBarFrame.Height;
#pragma warning restore CA1422
        // 刘海屏/全面屏机型（高度 ≥ 812pt）底部有 Home Indicator，约 34pt。
        var homeIndicatorHeight = UIScreen.MainScreen.Bounds.Height >= 812 ? 34 : 0;
        return new Thickness(0, statusBarHeight, 0, homeIndicatorHeight);
    }

    private UIWindow? GetHostWindow()
    {
        // HUD 子进程没有 UIScene，手动创建的窗口不会出现在 UIApplication.Windows 里，
        // 因此优先使用 AppDelegate 注入的窗口引用。
        if (HostWindow is { } hostWindow)
        {
            return hostWindow;
        }

        // Info.plist 没有配置 UIApplicationSceneManifest，App 仍走 AppDelegate 窗口生命周期，
        // 因此没有 UIWindowScene 可取，只能从应用级窗口列表获取 HUD 窗口。
#pragma warning disable CS0618, CA1422
        var windows = UIApplication.SharedApplication.Windows;
#pragma warning restore CS0618, CA1422
        return windows.FirstOrDefault();
    }
}
