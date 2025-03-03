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
using ClassIsland.Views;

namespace ClassIsland.Controls;

/// <summary>
/// RecoverySplashControl.xaml 的交互逻辑
/// </summary>
public partial class RecoverySplashControl : UserControl
{
    public RecoverySplashControl()
    {
        InitializeComponent();
            
    }

    private void RecoverySplashControl_OnLoaded(object sender, RoutedEventArgs e)
    {
        RecoveryWindow.Instance?.Dispatcher.InvokeAsync(() =>
        {
            RecoveryWindow.Instance.CompleteInitialized += InstanceOnCompleteInitialized;
        });
    }

    private void InstanceOnCompleteInitialized(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() => Visibility = Visibility.Collapsed);
    }
}