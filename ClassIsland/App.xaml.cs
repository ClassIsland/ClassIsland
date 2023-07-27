using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ClassIsland.Views;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Configuration;

namespace ClassIsland;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private CrashWindow? CrashWindow;
    private Mutex? Mutex;

    public App()
    {
    }

    public static string AppVersion
    {
        get;
    } = "1.0 pre-release 1";

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
            CrashInfo = e.Exception.ToString(),
            Exception = e.Exception
        };
        CrashWindow.ShowDialog();
    }

    private void App_OnStartup(object sender, StartupEventArgs e)
    {
        AppCenter.Start("7039a2b0-8b4e-4d2d-8d2c-3c993ec26514", typeof(Analytics), typeof(Crashes));
        AppCenter.SetEnabledAsync(false);
        Mutex = new Mutex(true, "ClassIsland.Lock", out var createNew);
        if (createNew)
        {
            return;
        }

        MessageBox.Show("应用已经在运行中，请勿重复启动第二个实例。");
        Environment.Exit(0);
    }

    public static void ReleaseLock()
    {
        var app = (App)Application.Current;
        app.Mutex?.ReleaseMutex();
    }
}
