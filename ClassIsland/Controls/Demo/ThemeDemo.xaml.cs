using System.Windows;
using System.Windows.Controls;

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