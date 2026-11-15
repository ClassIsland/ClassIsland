using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Primitives;
using Avalonia.iOS;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using AVFoundation;
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
    private static readonly TimeSpan BackgroundFrameInterval = TimeSpan.FromMilliseconds(33);
    private static readonly TimeSpan ForegroundFrameInterval = TimeSpan.FromMilliseconds(66);
    // 画中画开启后要等下一帧才会切换到更高的刷新率，间隔不宜过长。
    private static readonly TimeSpan HiddenFrameInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan SlowFrameThreshold = TimeSpan.FromMilliseconds(16);
    private static readonly TimeSpan UserStartFastFrameDuration = TimeSpan.FromSeconds(3);
    private const double ContentPadding = 4;
    // 主界面岛本身就是细长条（常见约 18:1），只防止更极端的比例；系统可能还会对画中画窗口的宽高比做自己的限制。
    private const double MaximumAspectRatio = 24;
    private const double FallbackScreenWidth = 390;
    // 画中画窗口放大后会比主界面岛的自然宽度更宽，按 3 倍渲染以免文字发虚。
    private const double MaximumRenderScale = 3;
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
    private double _islandScale = 1;
    private bool _isPrepared;
    private DateTime _fastFramesUntil;
    private DateTime _lastSlowFrameLog;
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
            Logger?.LogInformation("画中画悬浮主界面已启用（{Source}）", persist ? "用户打开开关" : "启动时恢复");
            // 只有用户亲手打开开关时才立即弹出画中画，作为开关生效的反馈。
            if (persist)
            {
                StartFromUserAction();
            }
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
            _frameTimer = new DispatcherTimer { Interval = HiddenFrameInterval };
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
        _fastFramesUntil = default;
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
        // 画面由 RenderTargetBitmap 在 UI 线程渲染，布局也由帧循环手动执行，这个不可见视图不需要自己的合成渲染。
        // 让它继续渲染会和应用主界面争用同一个渲染线程，并向一个看不见的 Metal 图层提交画面。
        if (TopLevel.GetTopLevel(root) is EmbeddableControlRoot hostRoot)
        {
            hostRoot.StopRendering();
        }
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
        _islandScale = 1;
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

        var stopwatch = Stopwatch.StartNew();
        UpdateCanvasLayout();
        EnqueueFrame();
        stopwatch.Stop();
        ReportSlowFrame(stopwatch.Elapsed);
        LogStateChanges();
        UpdateFrameInterval();
    }

    private void ReportSlowFrame(TimeSpan elapsed)
    {
        // 渲染在 UI 线程上执行，耗时过长会让应用界面卡顿；每分钟最多记录一次。
        if (elapsed < SlowFrameThreshold || DateTime.UtcNow - _lastSlowFrameLog < TimeSpan.FromMinutes(1))
        {
            return;
        }

        _lastSlowFrameLog = DateTime.UtcNow;
        Logger?.LogWarning("画中画单帧渲染耗时 {Elapsed} ms（{Width}x{Height} px）",
            (int)elapsed.TotalMilliseconds, _bitmap?.PixelSize.Width, _bitmap?.PixelSize.Height);
    }

    /// <summary>
    /// 画面紧贴主界面岛，只留少量边距，比屏幕宽时等比缩小。画中画窗口按画面宽高比开窗并把画面缩放到窗口大小，
    /// 画面越贴合，主界面岛在窗口里就越大；画面比主界面岛宽时，多出来的部分只会变成黑边。
    /// </summary>
    private void UpdateCanvasLayout()
    {
        _frame!.UpdateLayout();
        var content = _island!.IsVisible ? _island.DesiredSize : default;
        var canvas = _canvasSize;
        var scale = 1.0;
        if (content.Width >= 1 && content.Height >= 1)
        {
            var width = Math.Min(content.Width + ContentPadding * 2, GetScreenWidth());
            scale = Math.Min(1, (width - ContentPadding * 2) / content.Width);
            var height = content.Height * scale + ContentPadding * 2;
            canvas = new Size(width, Math.Max(height, width / MaximumAspectRatio));
        }

        if (Math.Abs(scale - _islandScale) > 0.001)
        {
            _islandScale = scale;
            _island.RenderTransformOrigin = RelativePoint.TopLeft;
            _island.RenderTransform = scale < 1 ? new ScaleTransform(scale, scale) : null;
        }
        Canvas.SetLeft(_island, (canvas.Width - content.Width * scale) / 2);
        Canvas.SetTop(_island, (canvas.Height - content.Height * scale) / 2);
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

    /// <summary>
    /// 系统只接受由用户操作触发的开启请求，帧计时器里的调用会被静默忽略，
    /// 因此在开关的点击处理中同步准备、送出首帧并发出请求。
    /// </summary>
    private void StartFromUserAction()
    {
        _fastFramesUntil = DateTime.UtcNow + UserStartFastFrameDuration;
        FrameTimerOnTick(null, EventArgs.Empty);
        if (!_isPrepared)
        {
            return;
        }

        var result = IosPipNativeBridge.Start();
        Logger?.LogInformation("已请求开启画中画：结果={Result}，{Message}", result, IosPipNativeBridge.GetLastError());
    }

    private void UpdateFrameInterval()
    {
        // 画中画显示时按动画需要的帧率刷新：应用在后台时 UI 线程空闲，用 30 fps；
        // 应用在前台时渲染会占用应用自己的 UI 线程，降到 15 fps。
        // 画中画未显示时没人看得到画面，只需保持显示层里有一帧较新的内容，供离开应用时自动开启使用；
        // 刚请求开启时多送几帧，系统需要收到内容后才允许开启画中画。
        var interval = _lastIsActive == true
            ? UIApplication.SharedApplication.ApplicationState == UIApplicationState.Background
                ? BackgroundFrameInterval
                : ForegroundFrameInterval
            : DateTime.UtcNow < _fastFramesUntil
                ? ForegroundFrameInterval
                : HiddenFrameInterval;
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

    private static double GetScreenWidth()
    {
        var width = (double)(FindWindow()?.Bounds.Width ?? 0);
        return width >= 1 ? width : FallbackScreenWidth;
    }

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
        Logger?.LogInformation("画中画状态：可开启={IsPossible}，正在显示={IsActive}，最近消息={Message}",
            isPossible, isActive, error);
    }
}
