using System;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ClassIsland.Controls.Demo;

/// <summary>
/// OpenTaskBarMenuDemo.xaml 的交互逻辑
/// </summary>
public partial class OpenTaskBarMenuDemo : UserControl
{
    public OpenTaskBarMenuDemo()
    {
        InitializeComponent();
    }

    protected override void OnInitialized(EventArgs e)
    {
        BeginStoryboard((Storyboard)FindResource("Loop")!);
        base.OnInitialized(e);
    }
}