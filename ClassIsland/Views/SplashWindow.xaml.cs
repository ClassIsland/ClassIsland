using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ClassIsland.Services;

namespace ClassIsland.Views;

/// <summary>
/// SplashWindow.xaml 的交互逻辑
/// </summary>
public partial class SplashWindow : Window
{
    public SplashService SplashService { get; }

    public SplashWindow(SplashService splashService)
    {
        SplashService = splashService;
        InitializeComponent();
    }

    public bool IsRendered => _isInit1 && _isInit2;

    private bool _isInit1 = false;
    private bool _isInit2 = false;

    protected override void OnContentRendered(EventArgs e)
    {
        var hWnd = new WindowInteropHelper(this).Handle;
        var style = NativeWindowHelper.GetWindowLong(hWnd, NativeWindowHelper.GWL_EXSTYLE);
        style |= NativeWindowHelper.WS_EX_TOOLWINDOW;
        var r = NativeWindowHelper.SetWindowLong(hWnd, NativeWindowHelper.GWL_EXSTYLE, style | NativeWindowHelper.WS_EX_TRANSPARENT);
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