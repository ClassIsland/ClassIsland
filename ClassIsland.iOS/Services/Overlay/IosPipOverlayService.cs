using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.iOS;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AVFoundation;
using ClassIsland.Controls;
using ClassIsland.iOS.Controls;
using ClassIsland.Models;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Services;
using ClassIsland.Shared;
using CoreGraphics;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;

namespace ClassIsland.iOS.Services.Overlay;

/// <summary>
/// 侧载发行版的画中画悬浮主界面服务。
/// 在主进程中离屏渲染 <see cref="IosMainWindowIslandView"/>，把每一帧送入系统画中画窗口；
/// 启用后离开应用时系统会自动弹出画中画，课表状态与应用内实时同步。
/// </summary>
[SupportedOSPlatform("ios15.0")]
internal sealed class IosPipOverlayService : IMainWindowOverlayService
{
    private static readonly TimeSpan IdleFrameInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan NotificationFrameInterval = TimeSpan.FromMilliseconds(66);
    // 用户刚打开开关时等待画中画变为可用的最长时间；系统需要先收到几帧内容才会允许开启。
    private static readonly TimeSpan ManualStartTimeout = TimeSpan.FromSeconds(3);
    private const double ContentPadding = 8;
    // 主界面通常很宽，画中画窗口会按内容比例缩放；限制宽高比，避免窗口缩成一条细线。
    private const double MaximumAspectRatio = 3;
    private const double MaximumRenderScale = 2;
    private const int MaximumPixelWidth = 2048;
    private static readonly Size EmptyCanvasSize = new(240, 80);

    private ILogger<IosPipOverlayService>? _logger;
    private bool? _isSupported;
    private bool? _lastIsPossible;
    private bool? _lastIsActive;
    private string? _lastStateError;
    private bool _isEnabled;
    private SettingsService? _settingsService;
    private DispatcherTimer? _frameTimer;
    private AvaloniaView? _hostView;
    private Border? _frame;
    private Canvas? _stage;
    private IosMainWindowIslandView? _island;
    private RenderTargetBitmap? _bitmap;
    private byte[]? _pixels;
    private Size _canvasSize = EmptyCanvasSize;
    private bool _isPrepared;
    private DateTime? _manualStartDeadline;
    private string? _lastLoggedError;

    /// <inheritdoc />
    public bool IsSupported => _isSupported ??= IosPipNativeBridge.IsSupported() == 1;

    /// <inheritdoc />
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetEnabled(value, persist: true);
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 在应用启动完成后恢复上次的开关状态。恢复时不主动弹出画中画，等用户离开应用时由系统自动开启。
    /// </summary>
    public void ResumeIfEnabled()
    {
        if (!_isEnabled && GetSettingsService()?.Settings.IsIosPipOverlayEnabled == true)
        {
            SetEnabled(true, persist: false);
        }
    }

    public void Shutdown()
    {
        if (_isEnabled)
        {
            StopRendering();
        }

        if (_settingsService != null)
        {
            _settingsService.PropertyChanged -= SettingsServiceOnPropertyChanged;
        }
    }

    private void SetEnabled(bool value, bool persist)
    {
        if (value == _isEnabled)
        {
            if (persist)
            {
                PersistSetting(value);
            }

            return;
        }

        if (value)
        {
            if (!IsSupported || !TryStartRendering())
            {
                // 启动失败时通知 UI 回到未启用状态，避免开关显示为已开启但画中画不可用。
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
                return;
            }

            _isEnabled = true;
            // 只有用户亲手打开开关时才立即弹出画中画，作为开关生效的反馈。
            _manualStartDeadline = persist ? DateTime.UtcNow + ManualStartTimeout : null;
            Logger?.LogInformation("画中画悬浮主界面已启用（{Source}）", persist ? "用户打开开关" : "启动时恢复");
        }
        else
        {
            _isEnabled = false;
            StopRendering();
        }

        if (persist)
        {
            PersistSetting(value);
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
    }

    private bool TryStartRendering()
    {
        try
        {
            if (GetSettingsService() is { } settingsService)
            {
                settingsService.PropertyChanged -= SettingsServiceOnPropertyChanged;
                settingsService.PropertyChanged += SettingsServiceOnPropertyChanged;
            }

            CreateHost();
            _frameTimer = new DispatcherTimer { Interval = IdleFrameInterval };
            _frameTimer.Tick += FrameTimerOnTick;
            _frameTimer.Start();
            return true;
        }
        catch (Exception exception)
        {
            Logger?.LogError(exception, "无法启动画中画悬浮主界面");
            StopRendering();
            return false;
        }
    }

    private void StopRendering()
    {
        _frameTimer?.Stop();
        _frameTimer = null;
        _manualStartDeadline = null;
        if (_isPrepared)
        {
            IosPipNativeBridge.Teardown();
            _isPrepared = false;
        }

        DestroyHost();
        _bitmap?.Dispose();
        _bitmap = null;
        _pixels = null;
        _canvasSize = EmptyCanvasSize;
    }

    /// <summary>
    /// 主界面岛需要真实的视觉树根才能加载组件、触发 Loaded 和应用主题，
    /// 因此放进一个独立的、不可见的 AvaloniaView，而不是挂到应用主视图上：
    /// 主界面岛初始化时会接管所在顶层的 DataContext 与主题宿主。
    /// </summary>
    private void CreateHost()
    {
        _island = new IosMainWindowIslandView();
        _stage = new Canvas();
        _stage.Children.Add(_island);
        _frame = new Border
        {
            Background = Brushes.Black,
            Width = _canvasSize.Width,
            Height = _canvasSize.Height,
            Child = _stage
        };
        // 外层 Canvas 以无限尺寸测量子元素，主界面岛可以按自然宽度排版而不受宿主视图尺寸限制。
        var root = new Canvas();
        root.Children.Add(_frame);
        _hostView = new AvaloniaView
        {
            Frame = new CGRect(0, 0, 1, 1),
            Alpha = 0,
            UserInteractionEnabled = false,
            Content = root
        };
        FindWindow()?.InsertSubview(_hostView, 0);
    }

    private void DestroyHost()
    {
        _island?.ReleaseHost();
        if (_hostView != null)
        {
            _hostView.Content = null;
            _hostView.RemoveFromSuperview();
            _hostView.Dispose();
        }

        _hostView = null;
        _frame = null;
        _stage = null;
        _island = null;
    }

    private void FrameTimerOnTick(object? sender, EventArgs e)
    {
        try
        {
            RenderFrame();
        }
        catch (Exception exception)
        {
            LogOnce("渲染画中画帧时发生异常", exception);
        }
    }

    private void RenderFrame()
    {
        if (_frame == null || _island == null || _stage == null || _hostView == null)
        {
            return;
        }

        if (_hostView.Superview == null)
        {
            FindWindow()?.InsertSubview(_hostView, 0);
        }

        if (!_isPrepared)
        {
            var prepareResult = IosPipNativeBridge.Prepare();
            if (prepareResult != IosPipNativeBridge.Ok)
            {
                // 窗口尚未就绪时会返回 NotPrepared，下一帧重试即可；仍记录一次，便于排查一直准备不了的情况。
                LogOnce($"无法准备画中画（{prepareResult}）：{IosPipNativeBridge.GetLastError()}");
                return;
            }

            _isPrepared = true;
            IosPipNativeBridge.SetAutoStart(1);
            var session = AVAudioSession.SharedInstance();
            Logger?.LogInformation(
                "画中画已准备：后台模式={BackgroundModes}，音频类别={Category}，选项={Options}",
                NSBundle.MainBundle.ObjectForInfoDictionary("UIBackgroundModes")?.Description ?? "（未声明）",
                session.Category,
                session.CategoryOptions);
        }

        UpdateCanvasLayout();
        EnqueueFrame();
        TryManualStart();
        LogStateChanges();
        UpdateFrameInterval();
    }

    private void UpdateCanvasLayout()
    {
        _frame!.UpdateLayout();
        var content = _island!.IsVisible ? _island.DesiredSize : default;
        var canvas = content.Width < 1 || content.Height < 1
            ? _canvasSize
            : new Size(content.Width + ContentPadding * 2, content.Height + ContentPadding * 2);
        if (canvas.Width / canvas.Height > MaximumAspectRatio)
        {
            canvas = canvas.WithHeight(canvas.Width / MaximumAspectRatio);
        }

        Canvas.SetLeft(_island, (canvas.Width - content.Width) / 2);
        Canvas.SetTop(_island, (canvas.Height - content.Height) / 2);
        if (canvas != _canvasSize)
        {
            _canvasSize = canvas;
            _frame.Width = canvas.Width;
            _frame.Height = canvas.Height;
        }

        _frame.UpdateLayout();
    }

    private void EnqueueFrame()
    {
        var scale = Math.Min(MaximumRenderScale, MaximumPixelWidth / _canvasSize.Width);
        var pixelSize = new PixelSize(
            Math.Max(1, (int)Math.Ceiling(_canvasSize.Width * scale)),
            Math.Max(1, (int)Math.Ceiling(_canvasSize.Height * scale)));
        if (_bitmap == null || _bitmap.PixelSize != pixelSize)
        {
            _bitmap?.Dispose();
            _bitmap = new RenderTargetBitmap(pixelSize, new Vector(96 * scale, 96 * scale));
            _pixels = new byte[pixelSize.Width * pixelSize.Height * 4];
        }

        _bitmap.Render(_frame!);
        var stride = pixelSize.Width * 4;
        var handle = GCHandle.Alloc(_pixels!, GCHandleType.Pinned);
        try
        {
            var address = handle.AddrOfPinnedObject();
            _bitmap.CopyPixels(new PixelRect(pixelSize), address, _pixels!.Length, stride);
            if (_bitmap.Format == PixelFormats.Rgba8888)
            {
                SwapRedAndBlue(_pixels);
            }

            var result = IosPipNativeBridge.EnqueueBgra(address, pixelSize.Width, pixelSize.Height, stride);
            if (result != IosPipNativeBridge.Ok)
            {
                LogOnce($"无法送出画中画帧：{IosPipNativeBridge.GetLastError()}");
            }
        }
        finally
        {
            handle.Free();
        }
    }

    private void TryManualStart()
    {
        if (_manualStartDeadline is not { } deadline)
        {
            return;
        }

        if (DateTime.UtcNow > deadline)
        {
            _manualStartDeadline = null;
            LogOnce($"系统未允许立即开启画中画，将在离开应用时自动开启：{IosPipNativeBridge.GetLastError()}");
            return;
        }

        if (IosPipNativeBridge.IsActive() == 1 || IosPipNativeBridge.IsPossible() != 1)
        {
            return;
        }

        _manualStartDeadline = null;
        var result = IosPipNativeBridge.Start();
        Logger?.LogInformation("已请求开启画中画：结果={Result}，{Error}", result, IosPipNativeBridge.GetLastError());
    }

    private void UpdateFrameInterval()
    {
        var isNotificationShowing = _island!.GetVisualDescendants()
            .OfType<MainWindowLine>()
            .Any(x => x.IsOverlayOpen);
        var interval = isNotificationShowing || _manualStartDeadline != null
            ? NotificationFrameInterval
            : IdleFrameInterval;
        if (_frameTimer != null && _frameTimer.Interval != interval)
        {
            _frameTimer.Interval = interval;
        }
    }

    private static void SwapRedAndBlue(byte[] pixels)
    {
        for (var i = 0; i < pixels.Length; i += 4)
        {
            (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
        }
    }

    private void SettingsServiceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 导入或恢复配置会替换整个 Settings 对象；主界面岛在构造时绑定 Settings，需要重建。
        if (e.PropertyName != nameof(SettingsService.Settings) || !_isEnabled)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (!_isEnabled)
            {
                return;
            }

            DestroyHost();
            CreateHost();
        });
    }

    private void PersistSetting(bool value)
    {
        if (GetSettingsService() is not { } settingsService)
        {
            return;
        }

        settingsService.Settings.IsIosPipOverlayEnabled = value;
    }

    private SettingsService? GetSettingsService() =>
        _settingsService ??= IAppHost.TryGetService<SettingsService>();

    private static UIWindow? FindWindow()
    {
        var windows = UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIWindowScene>()
            .SelectMany(x => x.Windows)
            .ToList();
        return windows.FirstOrDefault(x => x.IsKeyWindow) ?? windows.FirstOrDefault();
    }

    private void LogOnce(string message, Exception? exception = null)
    {
        // 帧循环每秒运行多次，相同错误只记录一次，避免刷满日志。
        if (message == _lastLoggedError)
        {
            return;
        }

        _lastLoggedError = message;
        Logger?.LogWarning(exception, "{Message}", message);
    }

    // 服务在应用宿主创建前就已构造，只能在使用时再解析日志记录器。
    private ILogger<IosPipOverlayService>? Logger =>
        _logger ??= IAppHost.TryGetService<ILogger<IosPipOverlayService>>();

    private void LogStateChanges()
    {
        var isPossible = IosPipNativeBridge.IsPossible() == 1;
        var isActive = IosPipNativeBridge.IsActive() == 1;
        var error = IosPipNativeBridge.GetLastError();
        if (isPossible == _lastIsPossible && isActive == _lastIsActive && error == _lastStateError)
        {
            return;
        }

        _lastIsPossible = isPossible;
        _lastIsActive = isActive;
        _lastStateError = error;
        Logger?.LogInformation("画中画状态：可开启={IsPossible}，正在显示={IsActive}，最近错误={Error}",
            isPossible, isActive, error);
    }
}
