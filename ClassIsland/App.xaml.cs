using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ClassIsland.Views;

namespace ClassIsland;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private CrashWindow? CrashWindow;

    public static string AppVersion
    {
        get;
    } = "0.3";

    private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        if (CrashWindow != null)
        {
            CrashWindow = null;
            GC.Collect();
        }

        CrashWindow = new CrashWindow()
        {
            CrashInfo = e.Exception.ToString()
        };
        CrashWindow.ShowDialog();
    }
}
