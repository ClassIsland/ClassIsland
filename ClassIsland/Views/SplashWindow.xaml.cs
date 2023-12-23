using System;
using System.Collections.Generic;
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

namespace ClassIsland.Views;

/// <summary>
/// SplashWindow.xaml 的交互逻辑
/// </summary>
public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    public bool IsRendered { get; set; } = false;

    protected override void OnContentRendered(EventArgs e)
    {
        var hWnd = new WindowInteropHelper(this).Handle;
        var style = NativeWindowHelper.GetWindowLong(hWnd, NativeWindowHelper.GWL_EXSTYLE);
        style |= NativeWindowHelper.WS_EX_TOOLWINDOW;
        var r = NativeWindowHelper.SetWindowLong(hWnd, NativeWindowHelper.GWL_EXSTYLE, style | NativeWindowHelper.WS_EX_TRANSPARENT);
        base.OnContentRendered(e);
        //IsRendered = true;
    }

    private void AsyncBox_OnLoadingViewLoaded(object? sender, EventArgs e)
    {
        IsRendered = true;
    }
}