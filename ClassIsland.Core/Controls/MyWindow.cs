using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Labs.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering;
using ClassIsland.Shared;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Commands;
using ClassIsland.Core.Models.Theming;
using FluentAvalonia.UI.Windowing;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 通用窗口基类
/// </summary>
public class MyWindow : AppWindow
{
    private bool _isAdornerAdded;

    /// <summary>
    /// 是否显示开源警告水印
    /// </summary>
    public static bool ShowOssWatermark { get; internal set; } = false;

    private bool _enableMicaWindow;

    private int _debugGraphState = 0;

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

        IsMicaSupported = OperatingSystem.IsWindows() && Environment.OSVersion.Version.Build > 22000;
        Loaded += OnLoaded;
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.F3)
        {
            return;
        }

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
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var commands = CommandManager.GetCommandBindings(this);
        commands.Add(new CommandBinding(UriNavigationCommands.UriNavigationCommand,
            (_, args) => IAppHost.TryGetService<IUriNavigationService>()
                ?.NavigateWrapped(new Uri(args.Parameter?.ToString() ?? "classisland:")),
            (_, args) => args.CanExecute = true));
        CommandManager.SetCommandBindings(this, commands);
        
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
        var appToastAdorner = new AppToastAdorner(this);
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