using System.Diagnostics;
using System.Reflection;
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
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Theming;
using FluentAvalonia.UI.Windowing;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 通用窗口基类
/// </summary>
[PseudoClasses(":no-easter-eggs")]
public class MyWindow : AppWindow
{
    
    private bool _isAdornerAdded;

    /// <summary>
    /// 是否显示开源警告水印
    /// </summary>
    public static bool ShowOssWatermark { get; internal set; } = false;

    private bool _enableMicaWindow;

    private int _debugGraphState = 0;

    private bool _suppressTouchMode = false;

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

    private AppToastAdorner? _appToastAdorner;

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
        
        IsMicaSupported = OperatingSystem.IsWindows() && Environment.OSVersion.Version >= WindowsVersions.Win11V21H2;
        Initialized += OnInitialized;
        Loaded += OnLoaded;
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
        KeyDown += OnKeyDown;
        PointerPressed += OnPointerUpdated;
        var managementService = IAppHost.Host?.Services.GetService(typeof(IManagementService)) as IManagementService;
        if (managementService?.Policy.DisableEasterEggs == true)
        {
            PseudoClasses.Add(":no-easter-eggs");
        }
        // PointerMoved += OnPointerUpdated;
    }
    
    private void OnPointerUpdated(object? sender, PointerEventArgs e)
    {
        PointerStateAssist.SetIsTouchMode(this, _suppressTouchMode | e.Pointer.Type == PointerType.Touch);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F3:
            {
                if (_debugGraphState == 0)
                {
                    _debugGraphState = (e.KeyModifiers & KeyModifiers.Shift) == KeyModifiers.Shift ? 2 : 1;
                }
                else
                {
                    _debugGraphState = 0;
                }

                RendererDiagnostics.DebugOverlays = _debugGraphState switch
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
                if (PointerStateAssist.GetIsTouchMode(this))
                {
                    PointerStateAssist.SetIsTouchMode(this, false);
                    _suppressTouchMode = false;
                }
                else
                {
                    PointerStateAssist.SetIsTouchMode(this, true);
                    if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    {
                        _suppressTouchMode = true;
                    }
                }
                this.ShowToast($"(debug) IsTouchMode={PointerStateAssist.GetIsTouchMode(this)}, Suppress={_suppressTouchMode}");
                break;
            case Key.F7 when _appToastAdorner != null:
                foreach (var message in _appToastAdorner.Messages)
                {
                    message.Close();
                }
                break;
        }
    }

    private void OnInitialized(object? sender, EventArgs e)
    {
        var commands = CommandManager.GetCommandBindings(this);
        commands.Add(new CommandBinding(UriNavigationCommands.UriNavigationCommand,
            (_, args) => IAppHost.TryGetService<IUriNavigationService>()
                ?.NavigateWrapped(new Uri(args.Parameter?.ToString() ?? "classisland:")),
            (_, args) => args.CanExecute = true));
        CommandManager.SetCommandBindings(this, commands);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (EnableMicaWindow && IsMicaSupported)
        {
            TransparencyLevelHint = [WindowTransparencyLevel.Mica];
            Background = Brushes.Transparent;
        }

        if (Content is not Control element || _isAdornerAdded)
        {
            return;
        }

        var layer = AdornerLayer.GetAdornerLayer(element);
        var appToastAdorner = _appToastAdorner = new AppToastAdorner(this);
        layer?.Children.Add(appToastAdorner);
        AdornerLayer.SetAdornedElement(appToastAdorner, this);
        
        if ((AppBase.Current.IsDevelopmentBuild || ShowOssWatermark))
        {
            var adorner = new DevelopmentBuildAdorner(AppBase.Current.IsDevelopmentBuild, ShowOssWatermark);
            layer?.Children.Add(adorner);
            AdornerLayer.SetAdornedElement(adorner, this);
        }
        _isAdornerAdded = true;
    }

}