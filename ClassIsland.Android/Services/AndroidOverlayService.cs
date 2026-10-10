using System.ComponentModel;
using System.Reactive.Disposables;
using System.Runtime.Versioning;
using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Android.Hardware.Display;
using Android.Hardware.Input;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Avalonia;
using Avalonia.Android;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Android.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models;
using ClassIsland.Models.Rules;
using ClassIsland.Core.Models.Ruleset;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Enums;
using ClassIsland.ViewModels;
using Microsoft.Extensions.Logging;
using AndroidSettings = global::Android.Provider.Settings;
using NativeView = global::Android.Views.View;
using PixelFormat = global::Android.Graphics.Format;

namespace ClassIsland.Android.Services;

[SupportedOSPlatform("android24.0")]
internal sealed class AndroidOverlayService : IDisposable
{
    private readonly Context _context;
    private ContextThemeWrapper? _windowContext;
    private IWindowManager? _windowManager;
    private readonly SettingsService _settingsService = IAppHost.GetService<SettingsService>();
    private readonly IXamlThemeService _themes = IAppHost.GetService<IXamlThemeService>();
    private readonly IRulesetService _rulesetService = IAppHost.GetService<IRulesetService>();
    private readonly Ruleset _onClassRules = new()
    {
        Groups =
        [
            new RuleGroup
            {
                Rules =
                [
                    new Rule
                    {
                        Id = "classisland.lessons.timeState",
                        Settings = new TimeStateRuleSettings { State = TimeState.OnClass }
                    }
                ]
            }
        ]
    };
    private readonly ILogger<AndroidOverlayService> _logger = IAppHost.GetService<ILogger<AndroidOverlayService>>();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Dictionary<object, double> _heightReservations = [];

    private Settings _settings;
    private AvaloniaView? _view;
    private AndroidMainView? _mainView;
    private Canvas? _canvas;
    private EmbeddableControlRoot? _root;
    private WindowManagerLayoutParams? _parameters;
    private double _reservedHeight;
    private double _contentHeight;
    private bool _isAttached;
    private bool _isUpdatingLayout;
    private bool _layoutQueued;
    private bool _disposed;
    private bool _showFailed;
    private bool _lastPermission;
    private bool _awaitingPermission;

    public AndroidOverlayService(Context service)
    {
        _context = service.ApplicationContext!;
        _settings = _settingsService.Settings;
        _settings.PropertyChanged += SettingsOnPropertyChanged;
        _settingsService.PropertyChanged += SettingsServiceOnPropertyChanged;
        _rulesetService.StatusUpdated += RulesetServiceOnStatusUpdated;
        MainActivity.Resumed += ActivityOnResumed;
        _timer.Tick += TimerOnTick;
        _timer.Start();
        Refresh();
    }

    private ContextThemeWrapper CreateOverlayContext()
    {
        var context = _context;
        var displays = context.GetSystemService(Context.DisplayService) as DisplayManager;
        if (displays?.GetDisplay(Display.DefaultDisplay) is { } display)
        {
            context = context.CreateDisplayContext(display)!;
        }
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            context = context.CreateWindowContext((int)WindowManagerTypes.ApplicationOverlay, null)!;
        }
        return new ContextThemeWrapper(context, ResourceConstant.Style.AppTheme);
    }

    private void SettingsServiceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SettingsService.Settings))
        {
            return;
        }
        Dispatcher.UIThread.Post(() =>
        {
            if (_disposed)
            {
                return;
            }
            _settings.PropertyChanged -= SettingsOnPropertyChanged;
            _settings = _settingsService.Settings;
            _settings.PropertyChanged += SettingsOnPropertyChanged;
            Hide();
            _showFailed = false;
            Refresh();
        });
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_disposed)
            {
                return;
            }
            if (e.PropertyName == nameof(Settings.IsAndroidOverlayEnabled))
            {
                _showFailed = false;
                _awaitingPermission = false;
                if (_settings.IsAndroidOverlayEnabled && !AndroidSettings.CanDrawOverlays(_context))
                {
                    RequestPermission();
                }
            }
            _mainView?.UpdateAppearance();
            Refresh();
        });
    }

    private void RequestPermission()
    {
        if (MainActivity.Current?.TryGetTarget(out var activity) != true ||
            activity.IsFinishing || !activity.IsForeground)
        {
            return;
        }
        try
        {
            using var intent = new Intent(AndroidSettings.ActionManageOverlayPermission,
                global::Android.Net.Uri.Parse($"package:{_context.PackageName}"));
            activity.StartActivity(intent);
            _awaitingPermission = true;
        }
        catch (ActivityNotFoundException e)
        {
            _logger.LogWarning(e, "无法打开悬浮窗授权设置");
            Toast.MakeText(_context, "请在系统设置中允许 ClassIsland 显示在其他应用上层。", ToastLength.Long)?.Show();
        }
    }

    private void ActivityOnResumed(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_disposed)
            {
                return;
            }
            _showFailed = false;
            if (_awaitingPermission)
            {
                _awaitingPermission = false;
                if (_settings.IsAndroidOverlayEnabled && !AndroidSettings.CanDrawOverlays(_context))
                {
                    Toast.MakeText(_context, "尚未允许显示在其他应用上层，悬浮主界面未显示。关闭后重新开启可再次授权。",
                        ToastLength.Long)?.Show();
                }
            }
            Refresh();
        });
    }

    private void TimerOnTick(object? sender, EventArgs e) => Refresh();

    private void RulesetServiceOnStatusUpdated(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(Refresh);
    }

    private void MainViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsNotificationWindowExplicitShowed))
        {
            Dispatcher.UIThread.Post(Refresh);
        }
    }

    private void Refresh()
    {
        if (_disposed)
        {
            return;
        }
        if (!_settings.IsAndroidOverlayEnabled || !_settings.IsMainWindowVisible)
        {
            Hide();
            return;
        }
        var permission = AndroidSettings.CanDrawOverlays(_context);
        if (permission != _lastPermission)
        {
            _showFailed = false;
            _lastPermission = permission;
        }
        if (!permission)
        {
            Hide();
            return;
        }
        if (_view == null && !_showFailed)
        {
            Show();
        }
        if (_mainView != null && _view != null)
        {
            var hide = _settings.HideMode switch
            {
                0 => _settings.HideOnClass && _rulesetService.IsRulesetSatisfied(_onClassRules),
                1 => _rulesetService.IsRulesetSatisfied(_settings.HideRules),
                _ => false
            };
            _mainView.ViewModel.IsHideRuleSatisfied = hide;
            // Keep the visual tree and notification consumers alive so topmost reminders can temporarily show it.
            _view.Visibility = hide && !_mainView.ViewModel.IsNotificationWindowExplicitShowed
                ? ViewStates.Invisible
                : ViewStates.Visible;
        }
        QueueLayout();
    }

    private void Show()
    {
        try
        {
            _windowContext = CreateOverlayContext();
            _windowManager = _windowContext.GetSystemService(Context.WindowService)!.JavaCast<IWindowManager>();
            _mainView = new AndroidMainView { ReserveHeight = ReserveRenderHeight };
            _mainView.ViewModel.PropertyChanged += MainViewModelOnPropertyChanged;
            _canvas = new Canvas { Background = Brushes.Transparent, ClipToBounds = false };
            _canvas.Children.Add(_mainView);
            _view = new AvaloniaView(_windowContext);
            _view.Content = _canvas;
            _root = TopLevel.GetTopLevel(_mainView) as EmbeddableControlRoot;
            _mainView.InitializeHost();
            ConfigureTransparency(_view);
            var type = OperatingSystem.IsAndroidVersionAtLeast(26)
                ? WindowManagerTypes.ApplicationOverlay
                : WindowManagerTypes.Phone;
            _parameters = new WindowManagerLayoutParams(1, 1, type,
                WindowManagerFlags.NotTouchable | WindowManagerFlags.NotFocusable |
                WindowManagerFlags.LayoutInScreen | WindowManagerFlags.LayoutNoLimits,
                PixelFormat.Translucent)
            {
                Gravity = GravityFlags.Top | GravityFlags.Left,
                Alpha = GetWindowAlpha()
            };
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                _parameters.FitInsetsTypes = 0;
            }
            _mainView.LayoutUpdated += MainViewOnLayoutUpdated;
            UpdateLayout();
            _windowManager.AddView(_view, _parameters);
            _isAttached = true;
            QueueLayout();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "无法显示 Android 悬浮主界面");
            Hide();
            _showFailed = true;
            Toast.MakeText(_context, "无法显示悬浮主界面。请检查显示在其他应用上层的权限，关闭后重新开启可重试。",
                ToastLength.Long)?.Show();
        }
    }

    private static void ConfigureTransparency(NativeView view)
    {
        if (view is SurfaceView surface)
        {
            surface.SetZOrderOnTop(true);
            surface.Holder?.SetFormat(PixelFormat.Translucent);
        }
        else if (view is TextureView texture)
        {
            texture.SetOpaque(false);
        }
        if (view is ViewGroup group)
        {
            for (var index = 0; index < group.ChildCount; index++)
            {
                if (group.GetChildAt(index) is { } child)
                {
                    ConfigureTransparency(child);
                }
            }
        }
    }

    private float GetWindowAlpha()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            return 1;
        }
        var input = _context.GetSystemService(Context.InputService) as InputManager;
        // Android tests the native window alpha, including fully transparent areas.
        return Math.Clamp(input?.MaximumObscuringOpacityForTouch ?? 0.8f, 0, 1);
    }

    private void MainViewOnLayoutUpdated(object? sender, EventArgs e) => QueueLayout();

    private void QueueLayout()
    {
        if (_layoutQueued || _view == null || _disposed)
        {
            return;
        }
        _layoutQueued = true;
        Dispatcher.UIThread.Post(() =>
        {
            _layoutQueued = false;
            if (_disposed || _view == null)
            {
                return;
            }
            try
            {
                UpdateLayout();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "无法更新 Android 悬浮窗布局");
                Hide();
                _showFailed = true;
            }
        }, DispatcherPriority.Background);
    }

    private global::Android.Graphics.Rect GetScreenBounds()
    {
        var windowManager = _windowManager!;
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var metrics = windowManager.MaximumWindowMetrics;
            var bounds = new global::Android.Graphics.Rect(metrics.Bounds);
            if (!_settings.IsIgnoreWorkAreaEnabled)
            {
                var insets = metrics.WindowInsets.GetInsetsIgnoringVisibility(
                    WindowInsets.Type.SystemBars() | WindowInsets.Type.DisplayCutout());
                bounds.Left += insets.Left;
                bounds.Top += insets.Top;
                bounds.Right -= insets.Right;
                bounds.Bottom -= insets.Bottom;
            }
            return bounds;
        }
        using var size = new global::Android.Graphics.Point();
        var display = windowManager.DefaultDisplay!;
        if (_settings.IsIgnoreWorkAreaEnabled)
        {
            display.GetRealSize(size);
            return new global::Android.Graphics.Rect(0, 0, size.X, size.Y);
        }
        display.GetSize(size);
        var resources = _windowContext!.Resources!;
        var statusBar = resources.GetIdentifier("status_bar_height", "dimen", "android");
        var top = statusBar == 0 ? 0 : resources.GetDimensionPixelSize(statusBar);
        var result = new global::Android.Graphics.Rect(0, top, size.X, size.Y);
        if (OperatingSystem.IsAndroidVersionAtLeast(28) && _view?.RootWindowInsets?.DisplayCutout is { } cutout)
        {
            result.Left = Math.Max(result.Left, cutout.SafeInsetLeft);
            result.Top = Math.Max(result.Top, cutout.SafeInsetTop);
            result.Right -= cutout.SafeInsetRight;
            result.Bottom -= cutout.SafeInsetBottom;
        }
        return result;
    }

    private void UpdateLayout()
    {
        if (_isUpdatingLayout || _mainView == null || _parameters == null || _canvas == null || _windowManager == null)
        {
            return;
        }
        _isUpdatingLayout = true;
        try
        {
            using var bounds = GetScreenBounds();
            var density = _root?.RenderScaling ?? _windowContext!.Resources!.DisplayMetrics!.Density;
            var width = Math.Max(1, bounds.Width());
            _mainView.Width = width / density;
            _mainView.Measure(new Size(width / density, double.PositiveInfinity));
            var scale = _settings.Scale;
            var dockingTop = _settings.WindowDockingLocation is 0 or 1 or 2;
            var safeArea = _themes.ActualVerticalSafeAreaPx;
            var offsetY = _settings.WindowDockingOffsetY / density;
            var safeTop = Math.Max(0, dockingTop ? Math.Min(safeArea, offsetY) : safeArea) * scale;
            var safeBottom = Math.Max(0, dockingTop ? safeArea : Math.Min(safeArea, -offsetY)) * scale;
            var contentHeight = _mainView.DesiredSize.Height + safeTop + safeBottom;
            _contentHeight = contentHeight;
            if (_heightReservations.Count > 0)
            {
                _reservedHeight = Math.Max(_reservedHeight, contentHeight);
            }
            var height = Math.Clamp(_heightReservations.Count == 0 ? contentHeight : _reservedHeight,
                1 / density, Math.Max(1 / density, bounds.Height() / density));
            var pixelHeight = Math.Max(1, (int)Math.Ceiling(height * density));
            var x = bounds.Left + _settings.WindowDockingOffsetX;
            var y = dockingTop
                ? bounds.Top + _settings.WindowDockingOffsetY - (int)Math.Round(safeTop * density)
                : bounds.Bottom + _settings.WindowDockingOffsetY - pixelHeight + (int)Math.Round(safeBottom * density);
            Canvas.SetTop(_mainView, safeTop + (dockingTop ? 0 : Math.Max(0, height - contentHeight)));
            var cutoutChanged = false;
            if (OperatingSystem.IsAndroidVersionAtLeast(28))
            {
                var cutoutMode = _settings.IsIgnoreWorkAreaEnabled
                    ? LayoutInDisplayCutoutMode.ShortEdges
                    : LayoutInDisplayCutoutMode.Never;
                cutoutChanged = _parameters.LayoutInDisplayCutoutMode != cutoutMode;
                _parameters.LayoutInDisplayCutoutMode = cutoutMode;
            }
            if (_parameters.Width == width && _parameters.Height == pixelHeight &&
                _parameters.X == x && _parameters.Y == y && !cutoutChanged)
            {
                return;
            }
            _parameters.Width = width;
            _parameters.Height = pixelHeight;
            _parameters.X = x;
            _parameters.Y = y;
            if (_isAttached)
            {
                _windowManager.UpdateViewLayout(_view, _parameters);
            }
        }
        finally
        {
            _isUpdatingLayout = false;
        }
    }

    private IDisposable ReserveRenderHeight(double additionalHeight)
    {
        var key = new object();
        _heightReservations[key] = Math.Max(0, additionalHeight);
        // Reserve the full entrance height once, rather than growing the surface on every animation frame.
        _reservedHeight = Math.Max(_reservedHeight, _contentHeight + _heightReservations.Values.Sum());
        UpdateLayout();
        return Disposable.Create(() =>
        {
            if (!_heightReservations.Remove(key))
            {
                return;
            }
            if (_heightReservations.Count == 0)
            {
                _reservedHeight = 0;
            }
            QueueLayout();
        });
    }

    private void Hide()
    {
        if (_mainView != null)
        {
            _mainView.LayoutUpdated -= MainViewOnLayoutUpdated;
            _mainView.ViewModel.PropertyChanged -= MainViewModelOnPropertyChanged;
            _root ??= TopLevel.GetTopLevel(_mainView) as EmbeddableControlRoot;
        }
        if (_isAttached && _view != null)
        {
            try
            {
                _windowManager?.RemoveViewImmediate(_view);
            }
            catch (Java.Lang.IllegalArgumentException e)
            {
                _logger.LogDebug(e, "悬浮窗已被系统移除");
            }
        }
        _isAttached = false;
        if (_view != null)
        {
            _view.Content = null;
        }
        _mainView?.ReleaseHost();
        _root?.Dispose();
        ((Java.Lang.Object?)_view)?.Dispose();
        _parameters?.Dispose();
        _windowContext?.Dispose();
        _windowContext = null;
        _windowManager = null;
        _view = null;
        _mainView = null;
        _canvas = null;
        _root = null;
        _parameters = null;
        _heightReservations.Clear();
        _reservedHeight = 0;
        _contentHeight = 0;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _timer.Stop();
        _timer.Tick -= TimerOnTick;
        _settings.PropertyChanged -= SettingsOnPropertyChanged;
        _settingsService.PropertyChanged -= SettingsServiceOnPropertyChanged;
        _rulesetService.StatusUpdated -= RulesetServiceOnStatusUpdated;
        MainActivity.Resumed -= ActivityOnResumed;
        Hide();
    }
}
