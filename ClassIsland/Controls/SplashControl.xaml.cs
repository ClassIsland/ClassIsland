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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.Services;

namespace ClassIsland.Controls;

/// <summary>
/// SplashControl.xaml 的交互逻辑
/// </summary>
public partial class SplashControl : UserControl
{
    public SplashService SplashService { get; } = App.GetService<SplashService>();
    public SettingsService SettingsService { get; } = App.GetService<SettingsService>();

    public SplashControl()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.Property == IsVisibleProperty && (bool)e.NewValue)
        {
            BeginStoryboard((Storyboard)FindResource("Intro"));
        }
        base.OnPropertyChanged(e);
    }
}