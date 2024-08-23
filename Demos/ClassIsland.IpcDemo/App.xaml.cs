using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;

namespace ClassIsland.IpcDemo;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.ToString(), "出错了", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void App_OnStartup(object sender, StartupEventArgs e)
    {

    }
}