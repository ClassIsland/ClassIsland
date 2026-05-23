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
public partial class MyWindow : AppWindow
{
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_PseudoClasses")]
    private static extern IPseudoClasses GetPseudoClasses(StyledElement element);

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
        AvaloniaProperty.RegisterAttached<MyWindow, Window, MyWindowState?>("State");

    internal static void SetState(Window obj, MyWindowState? value) => obj.SetValue(StateProperty, value);
    internal static MyWindowState? GetState(Window obj) => obj.GetValue(StateProperty);

    /// <summary>
    /// 为不继承 MyWindow 的类初始化 MyWindow 扩展特性。
    /// </summary>
    /// <param name="window">窗口</param>
    public static void SetupMyWindowExt(Window window)
    {
        var state = new MyWindowState();
        SetState(window, state);
        window.Initialized += OnInitialized;
        window.Loaded += OnLoaded;
        RenderOptions.SetBitmapInterpolationMode(window, BitmapInterpolationMode.HighQuality);
        window.KeyDown += OnKeyDown;
        window.PointerPressed += OnPointerUpdated;
        window.Closed += WindowOnClosed;

        var managementService = IAppHost.Host?.Services.GetService(typeof(IManagementService)) as IManagementService;
        if (managementService?.Policy.DisableEasterEggs == true)
        {
            GetPseudoClasses(window).Add(":no-easter-eggs");
        }
        
        return;

        void WindowOnClosed(object? sender, EventArgs e)
        {
            window.Initialized -= OnInitialized;
            window.Loaded -= OnLoaded;
            window.KeyDown -= OnKeyDown;
            window.PointerPressed -= OnPointerUpdated;
            window.Closed -= WindowOnClosed;
        }

        void OnPointerUpdated(object? sender, PointerEventArgs e)
        {
            PointerStateAssist.SetIsTouchMode(window, state.SuppressTouchMode || e.Pointer.Type == PointerType.Touch);
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

                    window.RendererDiagnostics.DebugOverlays = state.DebugGraphState switch
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

        void OnInitialized(object? sender, EventArgs e)
        {
            var commands = CommandManager.GetCommandBindings(window);
            commands.Add(new CommandBinding(UriNavigationCommands.UriNavigationCommand,
                (_, args) => IAppHost.TryGetService<IUriNavigationService>()
                    ?.NavigateWrapped(new Uri(args.Parameter?.ToString() ?? "classisland:")),
                (_, args) => args.CanExecute = true));
            CommandManager.SetCommandBindings(window, commands);
        }

        void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (window.Content is not Control element || state.IsAdornerAdded)
            {
                return;
            }

            var layer = AdornerLayer.GetAdornerLayer(element);
            var appToastAdorner = state.AppToastAdorner = new AppToastAdorner(window);
            layer?.Children.Add(appToastAdorner);
            AdornerLayer.SetAdornedElement(appToastAdorner, window);

            if ((AppBase.Current.IsDevelopmentBuild || ShowOssWatermark))
            {
                var adorner = new DevelopmentBuildAdorner(AppBase.Current.IsDevelopmentBuild, ShowOssWatermark);
                layer?.Children.Add(adorner);
                AdornerLayer.SetAdornedElement(adorner, window);
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