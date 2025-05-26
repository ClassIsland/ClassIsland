using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ClassIsland.Core;

namespace ClassIsland;

public partial class AppNew : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        Dispatcher.UIThread.UnhandledException += UIThreadOnUnhandledException;
        var win = new Window();
        win.Show();
        base.OnFrameworkInitializationCompleted();
    }

    private void UIThreadOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        AppBase.Current.Dispatcher.Invoke(() =>
        {
            ((App)AppBase.Current).ProcessUnhandledException(e.Exception);
        });
    }
}