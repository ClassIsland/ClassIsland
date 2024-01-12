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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls.Demo;

/// <summary>
/// ThemeDemo.xaml 的交互逻辑
/// </summary>
public partial class ThemeDemo : UserControl
{
    public static readonly DependencyProperty ThemeModeProperty = DependencyProperty.Register(
        nameof(ThemeMode), typeof(ColorZoneMode), typeof(ThemeDemo), new PropertyMetadata(default(ColorZoneMode)));

    public ColorZoneMode ThemeMode
    {
        get { return (ColorZoneMode)GetValue(ThemeModeProperty); }
        set { SetValue(ThemeModeProperty, value); }
    }

    public ThemeDemo()
    {
        InitializeComponent();
    }
}