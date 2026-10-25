using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using ClassIsland.iOS.Services.Platform;
using ClassIsland.Services;
using ClassIsland.Shared;
using UIKit;

namespace ClassIsland.iOS.Controls;

/// <summary>
/// HUD 宿主进程的全屏透明根视图。它只承载悬浮主界面岛，窗口本身由 AppDelegate 交给原生桥
/// 注册为 SpringBoard 全局悬浮窗口，因此这里不直接操作窗口或原生桥。
/// </summary>
public partial class IosHudHost : UserControl
{
    private bool _isDockedTop;
    private bool _hasLoggedMissingWindow;

    /// <summary>
    /// 当前承载的悬浮主界面岛，未初始化时为 <see langword="null"/>。
    /// </summary>
    public IosMainWindowIslandView? Island { get; private set; }

    /// <summary>
    /// 悬浮主界面岛是否已完成初始化。
    /// </summary>
    public bool IsIslandInitialized => Island != null;

    public IosHudHost()
    {
        InitializeComponent();
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

        _isDockedTop = settingsService.Settings.WindowDockingLocation is 0 or 1 or 2;
        Island = new IosMainWindowIslandView
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = _isDockedTop ? VerticalAlignment.Top : VerticalAlignment.Bottom
        };
        Root.Children.Add(Island);
        UpdateSafeAreaMargin();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // 宿主可能先于 InitializeIsland 完成加载，这里只是补一次安全区计算。
        UpdateSafeAreaMargin();
    }

    private void UpdateSafeAreaMargin()
    {
        if (Island == null)
        {
            return;
        }
        if (GetHostWindow() is not { } window)
        {
            if (!_hasLoggedMissingWindow)
            {
                _hasLoggedMissingWindow = true;
                IosStartupDiagnostics.Write("HUD island safe area skipped: host window is unavailable");
            }
            return;
        }

        // Avalonia DIP 与 iOS point 在设备上等值，可以直接用作 Margin。
        var insets = window.SafeAreaInsets;
        Island.Margin = _isDockedTop
            ? new Thickness(0, insets.Top, 0, 0)
            : new Thickness(0, 0, 0, insets.Bottom);
    }

    private static UIWindow? GetHostWindow()
    {
        // Info.plist 没有配置 UIApplicationSceneManifest，App 仍走 AppDelegate 窗口生命周期，
        // 因此没有 UIWindowScene 可取，只能从应用级窗口列表获取 HUD 窗口。
#pragma warning disable CS0618, CA1422
        return UIApplication.SharedApplication.Windows.FirstOrDefault();
#pragma warning restore CS0618, CA1422
    }
}
