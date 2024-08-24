using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers.Native;
using ClassIsland.Services;

namespace ClassIsland.Views;

/// <summary>
/// SplashWindow.xaml 的交互逻辑
/// </summary>
public partial class SplashWindow : Window
{
    public ISplashService SplashService { get; }

    public SplashWindow(ISplashService splashService)
    {
        SplashService = splashService;
        InitializeComponent();
    }

    public bool IsRendered => _isInit1 && _isInit2;

    private bool _isInit1 = false;
    private bool _isInit2 = false;

    protected override void OnContentRendered(EventArgs e)
    {
        var hWnd = (HWND)new WindowInteropHelper(this).Handle;
        var style = GetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        style |= NativeWindowHelper.WS_EX_TOOLWINDOW;
        var r = SetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, style | NativeWindowHelper.WS_EX_TRANSPARENT);
        base.OnContentRendered(e);
        Console.WriteLine("splash window rendered.");
        //IsRendered = true;
        _isInit1 = true;
    }

    private void AsyncBox_OnLoadingViewLoaded(object? sender, EventArgs e)
    {
        _isInit2 = true;
        Console.WriteLine("splash loading view loaded.");
        Console.WriteLine(new StackTrace());
    }

    private void SplashWindow_OnClosed(object? sender, EventArgs e)
    {
        
    }
}