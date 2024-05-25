using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

using ClassIsland.Core.Interfaces.Controls;
using ClassIsland.Services;
using ClassIsland.ViewModels;

using Microsoft.Extensions.Logging;

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
        Show();
        Hide();
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
        if (effect is not FrameworkElement element)
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

    private void SetupEffectVisual(FrameworkElement visual1, INotificationEffectControl effect)
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
        Top = bounds.Top * scale;
        Left = bounds.Left * scale;
    }

    private void TopmostEffectWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        ((HwndSource)PresentationSource.FromVisual(this)).AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
        {
            //想要让窗口透明穿透鼠标和触摸等，需要同时设置 WS_EX_LAYERED 和 WS_EX_TRANSPARENT 样式，
            //确保窗口始终有 WS_EX_LAYERED 这个样式，并在开启穿透时设置 WS_EX_TRANSPARENT 样式
            //但是WPF窗口在未设置 AllowsTransparency = true 时，会自动去掉 WS_EX_LAYERED 样式（在 HwndTarget 类中)，
            //如果设置了 AllowsTransparency = true 将使用WPF内置的低性能的透明实现，
            //所以这里通过 Hook 的方式，在不使用WPF内置的透明实现的情况下，强行保证这个样式存在。
            if (msg == (int)0x007C && (long)wParam == (long)WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE)
            {
                var styleStruct = (NativeWindowHelper.StyleStruct)Marshal.PtrToStructure(lParam, typeof(NativeWindowHelper.StyleStruct));
                styleStruct.styleNew |= (int)NativeWindowHelper.WS_EX_LAYERED;
                Marshal.StructureToPtr(styleStruct, lParam, false);
                handled = true;
            }
            return IntPtr.Zero;
        });
    }

    private void TopmostEffectWindow_OnContentRendered(object? sender, EventArgs e)
    {
        var hWnd = (HWND)new WindowInteropHelper(this).Handle;
        var style = GetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        style |= NativeWindowHelper.WS_EX_TOOLWINDOW;
        var r = SetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, style | NativeWindowHelper.WS_EX_TRANSPARENT);
    }

    private void TopmostEffectWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
    }
}