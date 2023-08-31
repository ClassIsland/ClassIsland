using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.Services.NotificationProviders;
using ClassIsland.Views;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UpdateStatus = ClassIsland.Enums.UpdateStatus;

namespace ClassIsland;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private CrashWindow? CrashWindow;
    private Mutex? Mutex;
    public static IHost Host;

    public static T GetService<T>()
    {
        var s = Host.Services.GetService(typeof(T));
        if (s != null)
        {
            return (T)s;
        }
        
        throw new ArgumentException($"Service {typeof(T)} is null!");
    }

    public App()
    {
    }

    public ApplicationCommand ApplicationCommand
    {
        get;
        set;
    } = new();

    public static string AppVersion => Assembly.GetExecutingAssembly().GetName().Version!.ToString();

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

    private async void App_OnStartup(object sender, StartupEventArgs e)
    {
        AppCenter.Start("7039a2b0-8b4e-4d2d-8d2c-3c993ec26514", typeof(Analytics), typeof(Crashes));
        await AppCenter.SetEnabledAsync(false);
        var command = new RootCommand
        {
            new Option<string>(new []{"--updateReplaceTarget", "-urt"}, "更新时要替换的文件"),
            new Option<string>(new []{"--updateDeleteTarget", "-udt"}, "更新完成要删除的文件"),
            new Option<bool>(new []{"--waitMutex", "-m"}, "重复启动应用时，等待上一个实例退出而非直接退出应用。")
        };
        command.Handler = CommandHandler.Create((ApplicationCommand c) =>
        {
            ApplicationCommand = c;
        });
        await command.InvokeAsync(e.Args);
        
        Mutex = new Mutex(true, "ClassIsland.Lock", out var createNew);
        if (!createNew)
        {
            if (ApplicationCommand.WaitMutex)
            {
                Mutex.WaitOne();
            }
            else
            {
                MessageBox.Show("应用已经在运行中，请勿重复启动第二个实例。");
                Environment.Exit(0);
            }
        }

        if (ApplicationCommand.UpdateReplaceTarget != null)
        {
            //MessageBox.Show($"Update replace {ApplicationCommand.UpdateReplaceTarget}");
            UpdateService.ReplaceApplicationFile(ApplicationCommand.UpdateReplaceTarget);
            Process.Start(new ProcessStartInfo()
            {
                FileName = ApplicationCommand.UpdateReplaceTarget,
                ArgumentList = { "-udt", Environment.ProcessPath! }
            });
            ReleaseLock();
            Shutdown();
            return;
        }
        if (ApplicationCommand.UpdateDeleteTarget != null)
        {            
            //MessageBox.Show($"Update DELETE {ApplicationCommand.UpdateDeleteTarget}");
            UpdateService.RemoveUpdateTemporary(ApplicationCommand.UpdateDeleteTarget);
        }

        Host = Microsoft.Extensions.Hosting.Host.
            CreateDefaultBuilder().
            UseContentRoot(AppContext.BaseDirectory).
            ConfigureServices((context, services) =>
            {
                services.AddSingleton<SettingsService>();
                services.AddSingleton<UpdateService>();
                services.AddSingleton<TaskBarIconService>();
                services.AddSingleton<WallpaperPickingService>();
                services.AddSingleton<NotificationHostService>();
                services.AddSingleton<ThemeService>();
                // 提醒提供方
                services.AddHostedService<ClassNotificationProvider>();
                services.AddHostedService<AfterSchoolNotificationProvider>();
            }).Build();
        try
        {
            GetService<TaskBarIconService>().MainTaskBarIcon.ForceCreate();
        }
        catch
        {
            // ignored
        }
        if (ApplicationCommand.UpdateDeleteTarget != null)
        {
            GetService<SettingsService>().Settings.LastUpdateStatus = UpdateStatus.UpToDate;
            GetService<TaskBarIconService>().MainTaskBarIcon.ShowNotification("更新完成。", $"应用已更新到版本{AppVersion}");
        }
        GetService<UpdateService>().AppStartup();
        _ = GetService<WallpaperPickingService>().GetWallpaperAsync();
        MainWindow = new MainWindow();
        MainWindow.Show();

        await Host.StartAsync();
    }

    public static void ReleaseLock()
    {
        var app = (App)Application.Current;
        app.Mutex?.ReleaseMutex();
    }
}
