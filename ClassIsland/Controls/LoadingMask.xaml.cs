using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using ClassIsland.Models;
using ClassIsland.Services;

namespace ClassIsland.Controls;

/// <summary>
/// LoadingMask.xaml 的交互逻辑
/// </summary>
public partial class LoadingMask : UserControl
{
    private ThemeService ThemeService { get; } = App.GetService<ThemeService>();
    public HangService HangService { get; } = App.GetService<HangService>();

    public LoadingMask()
    {
        InitializeComponent();
        UpdateForeground();
    }

    private void UpdateForeground()
    {
        var isLightMode = ThemeService.CurrentRealThemeMode == 0;
        var black = Color.FromArgb(255, 48, 48, 48);
        var white = Color.FromArgb(255, 242, 242, 242);
        var primary = ThemeService.CurrentTheme?.PrimaryMid.Color ?? Colors.DodgerBlue;
        Dispatcher.Invoke(() =>
        {
            MetroProgressBar.Foreground = new SolidColorBrush(primary);
        });
    }

    private void ThemeServiceOnThemeUpdated(object? sender, ThemeUpdatedEventArgs e)
    {
        UpdateForeground();
    }
}