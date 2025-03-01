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
using ClassIsland.Core;
using ClassIsland.ViewModels;
using ClassIsland.Views.RecoveryPages;

namespace ClassIsland.Views;

/// <summary>
/// RecoveryWindow.xaml 的交互逻辑
/// </summary>
public partial class RecoveryWindow
{
    public static RecoveryWindow? Instance { get; private set; }

    public event EventHandler? CompleteInitialized;

    public RecoveryViewModel ViewModel { get; } = new();

    public RecoveryWindow()
    {
        Instance = this;
        InitializeComponent();
        DataContext = this;
    }

    private void RecoveryWindow_OnClosed(object? sender, EventArgs e)
    {
        AppBase.Current.Stop();
    }

    private void ButtonGoBack_OnClick(object sender, RoutedEventArgs e)
    {
        MainFrame.GoBack();
    }

    private void RecoveryWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        MainFrame.NavigationService.Navigated += NavigationServiceOnNavigated;

        var home = new HomePage();
        home.Loaded += (o, args) => CompleteInitialized?.Invoke(this, EventArgs.Empty);
        MainFrame.Content = home;
    }

    private void NavigationServiceOnNavigated(object sender, NavigationEventArgs e)
    {
        ViewModel.CanGoBack = MainFrame.CanGoBack;
    }
}