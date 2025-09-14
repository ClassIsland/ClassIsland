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
public partial class TopmostEffectWindow : Window
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
#if DEBUG
        // RendererDiagnostics.DebugOverlays = RendererDebugOverlays.Fps;
#endif
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

    public void PlayEffect(INotificationEffectControl effect)
    {
        Logger.LogInformation("播放顶层特效：{}", effect);
        if (effect is not Control element)
            return;
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

    public void UpdateWindowPos(Screen screen, double scale)
    {
        //if (!IsShowed)
        //    return;
        var fullscreen = App.GetService<MainWindow>().ViewModel.IsForegroundFullscreen;
        var bounds = fullscreen ? screen.Bounds : screen.WorkingArea;
        Width = bounds.Width * scale;
        Height = bounds.Height * scale;
        Position = new PixelPoint((int)(bounds.X * scale), (int)(bounds.Y * scale));
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

    public override void Show()
    {
        ShowActivated = false;
        ShowInTaskbar = false;
        Topmost = true;
        base.Show();
        PlatformServices.WindowPlatformService.SetWindowFeature(this, 
            WindowFeatures.Transparent | WindowFeatures.ToolWindow | WindowFeatures.Topmost | WindowFeatures.SkipManagement, true);
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        
    }
}

