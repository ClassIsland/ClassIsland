using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Rendering;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers.Native;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Enums;
using ClassIsland.Shared.Interfaces.Controls;
using ClassIsland.Services;
using ClassIsland.ViewModels;

using Microsoft.Extensions.Logging;
using Control = Avalonia.Controls.Control;

namespace ClassIsland.Views;

/// <summary>
/// TopmostEffectWindow.xaml 的交互逻辑
/// </summary>
public partial class TopmostEffectWindow : Window, ITopmostEffectPlayer
{
    public TopmostEffectWindowViewModel ViewModel { get; } = new();

    public bool IsShowed { get; set; } = false;

    private ILogger<TopmostEffectWindow> Logger { get; }

    public SettingsService SettingsService { get; }

    public TopmostEffectWindow(ILogger<TopmostEffectWindow> logger, SettingsService settingsService)
    {
        Logger = logger;
        SettingsService = settingsService;
        InitializeComponent();
        DataContext = this;
        ViewModel.EffectControls.CollectionChanged += EffectControlsOnCollectionChanged;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];

        // 关键：这两个属性必须在窗口句柄创建**之前**设置。
        // 上课动画是一个铺满整块屏幕的置顶窗口；如果它抢走激活状态，
        // 动画播放的这几分钟里，其它程序（课件、浏览器、输入框）就会收不到键盘输入，
        // 也就是"上课前几分钟运行上课动画时输入框无效"。
        ShowActivated = false;
        Focusable = false;
        ShowInTaskbar = false;
        Opened += (_, _) => EnsureClickThrough();
#if DEBUG
        // RendererDiagnostics.DebugOverlays = RendererDebugOverlays.Fps;
#endif
    }

    /// <summary>
    /// 确保顶层特效窗口"看得见、点不着"。
    /// </summary>
    /// <remarks>
    /// 仅调用一次平台服务是不够的：窗口句柄可能尚未创建，或者
    /// <c>TransparencyLevelHint</c> 在显示后又被 Avalonia 重建了合成层，
    /// 导致 <c>WS_EX_TRANSPARENT</c> 丢失。此时这个全屏置顶窗口会开始吞掉
    /// 所有鼠标点击，用户看到的就是"输入框点不动/没反应"。
    /// 这里在显示后、以及每次播放特效前都重新确认一次扩展样式。
    /// </remarks>
    private void EnsureClickThrough()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var handle = TryGetPlatformHandle()?.Handle ?? nint.Zero;
        if (handle == nint.Zero)
        {
            return;
        }

        try
        {
            PlatformServices.WindowPlatformService.SetWindowFeature(this,
                WindowFeatures.Transparent | WindowFeatures.ToolWindow | WindowFeatures.Topmost |
                WindowFeatures.SkipManagement, true);

            var exStyle = GetWindowExStyle(handle);
            const long required = WsExTransparent | WsExNoActivate | WsExLayered | WsExToolWindow;
            if ((exStyle & required) != required)
            {
                SetWindowExStyle(handle, exStyle | required);
                Logger.LogDebug("补齐顶层特效窗口的鼠标穿透样式：0x{Style:X} -> 0x{NewStyle:X}", exStyle,
                    GetWindowExStyle(handle));
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "设置顶层特效窗口鼠标穿透失败。");
        }
    }

    private void EffectControlsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Logger.LogDebug("EffectControls.Count = {}", ViewModel.EffectControls.Count);
        if (ViewModel.EffectControls.Count > 0)
        {
            if (IsShowed) return;
            Show();
            IsShowed = true;
            Logger.LogDebug("显示顶层效果窗口。");
        }
        else
        {
            if (!IsShowed) return;
            Hide();
            IsShowed = false;
            Logger.LogDebug("隐藏顶层效果窗口。");
        }
    }

    public override void Show()
    {
        ShowActivated = false;
        ShowInTaskbar = false;
        Topmost = true;
        base.Show();
        EnsureClickThrough();
        // 合成层可能在 Show 之后才创建完成，下一轮消息循环再确认一次。
        Avalonia.Threading.Dispatcher.UIThread.Post(EnsureClickThrough, Avalonia.Threading.DispatcherPriority.Background);
    }

    public void PlayEffect(INotificationEffectControl effect)
    {
        Logger.LogInformation("播放顶层特效：{}", effect);
        if (effect is not Control element)
            return;
        if (ViewModel.IsEditMode)
        {
            Logger.LogInformation("由于应用处于编辑模式，将丢弃当前顶层特效");
            return;
        }
        // 每次播放前都重新确认一次穿透样式，避免上一轮 Hide/Show 后样式丢失。
        EnsureClickThrough();
        ViewModel.EffectControls.Add(element);
        if (!element.IsLoaded)
        {
            element.Loaded += (sender, args) => SetupEffectVisual(element, effect);
        }
        else
        {
            SetupEffectVisual(element, effect);
        }
    }

    private void SetupEffectVisual(Control visual1, INotificationEffectControl effect)
    {
        effect.EffectCompleted += (sender, args) =>
        {
            Logger.LogInformation("结束播放并移除顶层特效：{}", effect);
            ViewModel.EffectControls.Remove(visual1);
        };
        effect.Play();
    }

    public void UpdateWindowPos(Screen screen, double scale, bool fullscreen)
    {
        //if (!IsShowed)
        //    return;
        var bounds = fullscreen ? screen.Bounds : screen.WorkingArea;
        Width = bounds.Width * scale - (fullscreen ? 1 : 0) ;  // 防止因全屏导致的奇奇怪怪的问题
        Height = bounds.Height * scale;
        Position = new PixelPoint(bounds.X, bounds.Y);
        // Logger.LogTrace("Updated Window Pos: w:{} h:{} x:{} y:{}", Width, Height, Position.X, Position.Y);
    }

    private void TopmostEffectWindow_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (e.CloseReason is WindowCloseReason.OSShutdown or WindowCloseReason.ApplicationShutdown)
        {
            return;
        }
        e.Cancel = true;
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        
    }

    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020;
    private const long WsExToolWindow = 0x00000080;
    private const long WsExLayered = 0x00080000;
    private const long WsExNoActivate = 0x08000000;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    private static long GetWindowExStyle(IntPtr hWnd) =>
        IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, GwlExStyle).ToInt64() : GetWindowLong32(hWnd, GwlExStyle);

    private static void SetWindowExStyle(IntPtr hWnd, long style)
    {
        if (IntPtr.Size == 8)
        {
            SetWindowLongPtr64(hWnd, GwlExStyle, (IntPtr)style);
        }
        else
        {
            SetWindowLong32(hWnd, GwlExStyle, (int)style);
        }
    }
}

