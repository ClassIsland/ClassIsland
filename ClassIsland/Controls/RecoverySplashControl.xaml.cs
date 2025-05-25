using System;
using System.Windows;
using System.Windows.Controls;
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