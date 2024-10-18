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
/// AuthorizeWindow.xaml 的交互逻辑
/// </summary>
public partial class AuthorizeWindow
{

    public AuthorizeWindow()
    {
        
        InitializeComponent();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        var result = SetWindowDisplayAffinity((HWND)new WindowInteropHelper(this).Handle, WINDOW_DISPLAY_AFFINITY.WDA_EXCLUDEFROMCAPTURE);
        base.OnContentRendered(e);
    }

    protected override void OnInitialized(EventArgs e)
    {
        

        base.OnInitialized(e);
    }
}