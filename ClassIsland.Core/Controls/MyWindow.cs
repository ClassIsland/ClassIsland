using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Labs.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ClassIsland.Shared;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Assists;
using ClassIsland.Core.Commands;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Theming;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Windowing;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 通用窗口基类
/// </summary>
[PseudoClasses(":no-easter-eggs")]
public partial class MyWindow : FAAppWindow
{
    private static readonly TimeSpan TouchToolTipAdditionalShowDelay = TimeSpan.FromMilliseconds(400);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_PseudoClasses")]
    private static extern IPseudoClasses GetPseudoClasses(StyledElement element);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_IsPressed")]
    private static extern void SetButtonIsPressed(Button button, bool value);

    /// <summary>
    /// 是否显示开源警告水印
    /// </summary>
    public static bool ShowOssWatermark { get; internal set; } = false;

    private bool _enableMicaWindow;
    
    /// <summary>
    /// 启用云母窗口背景的直接属性
    /// </summary>
    public static readonly DirectProperty<MyWindow, bool> EnableMicaWindowProperty = AvaloniaProperty.RegisterDirect<MyWindow, bool>(
        nameof(EnableMicaWindow), o => o.EnableMicaWindow, (o, v) => o.EnableMicaWindow = v);

    private bool _isMicaSupported;

    public static readonly DirectProperty<MyWindow, bool> IsMicaSupportedProperty = AvaloniaProperty.RegisterDirect<MyWindow, bool>(
        nameof(IsMicaSupported), o => o.IsMicaSupported, (o, v) => o.IsMicaSupported = v);

    public bool IsMicaSupported
    {
        get => _isMicaSupported;
        set => SetAndRaise(IsMicaSupportedProperty, ref _isMicaSupported, value);
    }

    
    /// <summary>
    /// 启用云母窗口背景
    /// </summary>
    public bool EnableMicaWindow
    {
        get => _enableMicaWindow;
        set => SetAndRaise(EnableMicaWindowProperty, ref _enableMicaWindow, value);
    }

    public static readonly AttachedProperty<MyWindowState?> StateProperty =
        AvaloniaProperty.RegisterAttached<MyWindow, Control, MyWindowState?>("State");

    internal static void SetState(Control obj, MyWindowState? value) => obj.SetValue(StateProperty, value);
    internal static MyWindowState? GetState(Control obj) => obj.GetValue(StateProperty);

    /// <summary>
    /// 为不继承 MyWindow 的类初始化 MyWindow 扩展特性。
    /// </summary>
    /// <param name="window">窗口</param>
    public static void SetupMyWindowExt(Control window)
    {
        var state = new MyWindowState();
        WeakReference<Control>? touchToolTipTarget = null;
        IDisposable? touchToolTipShowDelayTimer = null;
        IPointer? pendingTouchPointer = null;
        SetState(window, state);
        window.Loaded += OnLoaded;
        RenderOptions.SetBitmapInterpolationMode(window, BitmapInterpolationMode.HighQuality);
        window.KeyDown += OnKeyDown;
        window.AddHandler(PointerPressedEvent, OnPointerUpdated, RoutingStrategies.Bubble | RoutingStrategies.Tunnel);
        window.AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        window.AddHandler(HoldingEvent, OnHolding, RoutingStrategies.Bubble, handledEventsToo: true);
        window.Unloaded += WindowOnUnloaded;

        var managementService = IAppHost.Host?.Services.GetService(typeof(IManagementService)) as IManagementService;
        if (managementService?.Policy.DisableEasterEggs == true)
        {
            GetPseudoClasses(window).Add(":no-easter-eggs");
        }
        
        return;

        void WindowOnUnloaded(object? sender, EventArgs e)
        {
            CancelTouchToolTipShowDelay();
            CloseTouchToolTip();
            window.Loaded -= OnLoaded;
            window.KeyDown -= OnKeyDown;
            window.PointerPressed -= OnPointerUpdated;
            window.RemoveHandler(PointerReleasedEvent, OnPointerReleased);
            window.RemoveHandler(HoldingEvent, OnHolding);
            window.Unloaded -= WindowOnUnloaded;
        }

        void OnPointerUpdated(object? sender, PointerEventArgs e)
        {
            if (e.Pointer.Type == PointerType.Touch)
            {
                CancelTouchToolTipShowDelay();
                CloseTouchToolTip();
                pendingTouchPointer = e.Pointer;
            }

            PointerStateAssist.SetIsTouchMode(window, state.SuppressTouchMode || e.Pointer.Type == PointerType.Touch);
        }

        void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.Pointer.Type != PointerType.Touch)
            {
                return;
            }

            CancelTouchToolTipShowDelay();
            if (ReferenceEquals(pendingTouchPointer, e.Pointer))
            {
                pendingTouchPointer = null;
            }
        }

        void OnHolding(object? sender, HoldingRoutedEventArgs e)
        {
            if (e.PointerType != PointerType.Touch)
            {
                return;
            }

            if (e.HoldingState != HoldingState.Started)
            {
                CancelTouchToolTipShowDelay();
                return;
            }

            var ancestors = (e.Source as Visual)?.GetSelfAndVisualAncestors().OfType<Control>().ToList();
            var target = ancestors?
                .FirstOrDefault(control => ToolTip.GetTip(control) is not null && ToolTip.GetServiceEnabled(control));
            var pointer = pendingTouchPointer;
            if (target is null || pointer is null)
            {
                return;
            }

            var button = ancestors?.OfType<Button>().FirstOrDefault();
            CancelTouchToolTipShowDelay();
            touchToolTipShowDelayTimer = DispatcherTimer.RunOnce(() =>
            {
                touchToolTipShowDelayTimer = null;
                if (!ReferenceEquals(pendingTouchPointer, pointer) || !target.IsAttachedToVisualTree())
                {
                    return;
                }

                CloseTouchToolTip();
                target.SetCurrentValue(ToolTip.IsOpenProperty, true);
                if (!ToolTip.GetIsOpen(target))
                {
                    return;
                }

                touchToolTipTarget = new WeakReference<Control>(target);
                if (button is not null)
                {
                    // Avalonia Button 在释放时根据此状态触发 Click；仅在 ToolTip 成功打开后清除。
                    SetButtonIsPressed(button, false);
                }
            }, TouchToolTipAdditionalShowDelay);
        }

        void CancelTouchToolTipShowDelay()
        {
            touchToolTipShowDelayTimer?.Dispose();
            touchToolTipShowDelayTimer = null;
        }

        void CloseTouchToolTip()
        {
            if (touchToolTipTarget?.TryGetTarget(out var target) == true)
            {
                target.SetCurrentValue(ToolTip.IsOpenProperty, false);
            }

            touchToolTipTarget = null;
        }

        void OnKeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F3:
                {
                    if (state.DebugGraphState == 0)
                    {
                        state.DebugGraphState = (e.KeyModifiers & KeyModifiers.Shift) == KeyModifiers.Shift ? 2 : 1;
                    }
                    else
                    {
                        state.DebugGraphState = 0;
                    }

                    GetTopLevel(window)?.RendererDiagnostics.DebugOverlays = state.DebugGraphState switch
                    {
                        0 => RendererDebugOverlays.None,
                        1 => RendererDebugOverlays.Fps,
                        2 => RendererDebugOverlays.Fps | RendererDebugOverlays.LayoutTimeGraph |
                             RendererDebugOverlays.RenderTimeGraph,
                        _ => RendererDebugOverlays.None
                    };
                    break;
                }
                case Key.F6:
                    if (PointerStateAssist.GetIsTouchMode(window))
                    {
                        PointerStateAssist.SetIsTouchMode(window, false);
                        state.SuppressTouchMode = false;
                    }
                    else
                    {
                        PointerStateAssist.SetIsTouchMode(window, true);
                        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                        {
                            state.SuppressTouchMode = true;
                        }
                    }

                    window.ShowToast(
                        $"(debug) IsTouchMode={PointerStateAssist.GetIsTouchMode(window)}, Suppress={state.SuppressTouchMode}");
                    break;
                case Key.F7 when state.AppToastAdorner != null:
                    foreach (var message in state.AppToastAdorner.Messages)
                    {
                        message.Close();
                    }

                    break;
            }
        }

        void OnLoaded(object? sender, EventArgs e)
        {
            var commands = CommandManager.GetCommandBindings(window);
            commands.Add(new CommandBinding(UriNavigationCommands.UriNavigationCommand,
                (_, args) => IAppHost.TryGetService<IUriNavigationService>()
                    ?.NavigateWrapped(new Uri(args.Parameter?.ToString() ?? "classisland:")),
                (_, args) => args.CanExecute = true));
            CommandManager.SetCommandBindings(window, commands);
            if (window is not ContentControl { Content: Visual visual })
            {
                return;
            }

            if (visual.IsAttachedToVisualTree())
            {
                AddAdorners();
            }
            else
            {
                visual.AttachedToVisualTree += VisualOnAttachedToVisualTree;
            }
            
            return;
            
            void VisualOnAttachedToVisualTree(object? o, VisualTreeAttachmentEventArgs visualTreeAttachmentEventArgs)
            {
                visual.AttachedToVisualTree -= VisualOnAttachedToVisualTree;
                AddAdorners();
            }
        }
        
        void AddAdorners()
        {
            if (window is not ContentControl { Content: Visual element } || state.IsAdornerAdded)
            {
                return;
            }
            var layer = AdornerLayer.GetAdornerLayer(element);
            if (layer == null)
            {
                return;
            }
            var appToastAdorner = state.AppToastAdorner = new AppToastAdorner(GetTopLevel(window)!);
            layer.Children.Add(appToastAdorner);
            AdornerLayer.SetAdornedElement(appToastAdorner, element);

            if ((AppBase.Current.IsDevelopmentBuild || ShowOssWatermark))
            {
                var adorner = new DevelopmentBuildAdorner(AppBase.Current.IsDevelopmentBuild, ShowOssWatermark);
                layer?.Children.Add(adorner);
                AdornerLayer.SetAdornedElement(adorner, element);
            }

            state.IsAdornerAdded = true;
        }
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public MyWindow()
    {
        try
        {
            IAppHost.GetService<IHangService>().AssumeHang();
        }
        catch
        {
            // ignored
        }
        
        IsMicaSupported = OperatingSystem.IsWindows() 
                          && Environment.OSVersion.Version >= WindowsVersions.Win11V21H2
                          && AvaloniaUnsafeAccessorHelpers.GetActiveWin32CompositionMode() == Win32CompositionMode.WinUIComposition;
        Loaded += OnLoaded;
        SetupMyWindowExt(this);
        // PointerMoved += OnPointerUpdated;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (EnableMicaWindow && IsMicaSupported)
        {
            TransparencyLevelHint = [WindowTransparencyLevel.Mica];
            Background = Brushes.Transparent;
        }
    }

    /// <summary>
    /// <see cref="MyWindow"/> 的共享状态。
    /// </summary>
    public partial class MyWindowState : ObservableObject
    {
        [ObservableProperty] private bool _isAdornerAdded;
        [ObservableProperty] private bool _enableMicaWindow;
        [ObservableProperty] private int _debugGraphState = 0;
        [ObservableProperty] private bool _suppressTouchMode = false;
        [ObservableProperty] private AppToastAdorner? _appToastAdorner;
    }

}
