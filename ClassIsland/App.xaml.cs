using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Diagnostics;
using System.Windows.Threading;
using ClassIsland.Controls;
using ClassIsland.Controls.AttachedSettingsControls;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.Services.MiniInfoProviders;
using ClassIsland.Services.NotificationProviders;
using ClassIsland.Views;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UpdateStatus = ClassIsland.Enums.UpdateStatus;

namespace ClassIsland;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private CrashWindow? CrashWindow;
    private Mutex? Mutex;
    private ILogger<App>? Logger { get; set; }
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

    public static string AppCodeName => "Elysia";

    public static string AppVersionLong =>
        $"{AppVersion}-{AppCodeName}-{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})";

    private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        if (CrashWindow != null)
        {
            CrashWindow = null;
            GC.Collect();
        }
        Logger?.LogCritical(e.Exception, "发生严重错误。");

        var exps = new ObservableCollection<Exception>();
        var ex = e.Exception;
        while (ex != null)
        {
            exps.Add(ex);
            ex = ex.InnerException;
        }
        CrashWindow = new CrashWindow()
        {
            CrashInfo = e.Exception.ToString(),
            Exception = e.Exception,
            InnerExceptions = exps
        };
#if DEBUG
        if (e.Exception.GetType() != typeof(ResourceReferenceKeyNotFoundException))
        {
            CrashWindow.ShowDialog();
        }
#else
        CrashWindow.ShowDialog();
#endif
    }

    private async void App_OnStartup(object sender, StartupEventArgs e)
    {
        //DependencyPropertyHelper.ForceOverwriteDependencyPropertyDefaultValue(FrameworkElement.FocusVisualStyleProperty,
        //    Resources[SystemParameters.FocusVisualStyleKey]);
        //DependencyPropertyHelper.ForceOverwriteDependencyPropertyDefaultValue(ButtonBase.FocusVisualStyleProperty,
        //    Resources[SystemParameters.FocusVisualStyleKey]);

        //ConsoleService.InitializeConsole();
        ConsoleService.InitializeConsole();
        Console.WriteLine($"ClassIsland {AppVersionLong}");

        BindingDiagnostics.BindingFailed += BindingDiagnosticsOnBindingFailed;
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
                services.AddSingleton<MiniInfoProviderHostService>();
                services.AddSingleton<WeatherService>();
                services.AddSingleton<FileFolderService>();
                services.AddSingleton<AttachedSettingsHostService>();
                services.AddSingleton<ProfileService>();
                services.AddSingleton<SplashService>();
                services.AddSingleton<HangService>();
                services.AddSingleton<ConsoleService>();
                //services.AddHostedService<BootService>();
                // Views
                services.AddSingleton<MainWindow>();
                services.AddSingleton<SplashWindow>();
                // 提醒提供方
                services.AddHostedService<ClassNotificationProvider>();
                services.AddHostedService<AfterSchoolNotificationProvider>();
                services.AddHostedService<WeatherNotificationProvider>();
                // 简略信息提供方
                services.AddHostedService<DateMiniInfoProvider>();
                services.AddHostedService<WeatherMiniInfoProvider>();
                // Transients
                services.AddTransient<ExcelImportWindow>();
                services.AddTransient<WallpaperPreviewWindow>();
                // Logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                });
            }).Build();
        ConsoleService.ConsoleVisible = GetService<SettingsService>().Settings.IsDebugConsoleEnabled;
        //OverrideFocusVisualStyle();
        Logger = GetService<ILogger<App>>();
        Logger.LogInformation("初始化应用。");
        if (GetService<SettingsService>().Settings.IsSplashEnabled)
        {
            GetService<SplashWindow>().Show();
            await Task.Run(() =>
            {
                var b = false;
                while (!b)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, () =>
                    {
                        b = GetService<SplashWindow>().IsRendered;
                    });
                }
            });
        }

        GetService<HangService>();
        try
        {
            GetService<TaskBarIconService>().MainTaskBarIcon.ForceCreate(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "创建任务栏图标失败。");
        }
        if (ApplicationCommand.UpdateDeleteTarget != null)
        {
            GetService<SettingsService>().Settings.LastUpdateStatus = UpdateStatus.UpToDate;
            GetService<TaskBarIconService>().MainTaskBarIcon.ShowNotification("更新完成。", $"应用已更新到版本{AppVersion}");
        }
        GetService<UpdateService>().AppStartup();
        var attachedSettingsHostService = GetService<AttachedSettingsHostService>();
        attachedSettingsHostService.SubjectSettingsAttachedSettingsControls.Add(typeof(LessonControlAttachedSettingsControl));
        attachedSettingsHostService.ClassPlanSettingsAttachedSettingsControls.Add(typeof(LessonControlAttachedSettingsControl));
        attachedSettingsHostService.TimeLayoutSettingsAttachedSettingsControls.Add(typeof(LessonControlAttachedSettingsControl));
        attachedSettingsHostService.TimePointSettingsAttachedSettingsControls.Add(typeof(LessonControlAttachedSettingsControl));

        GetService<WeatherService>();
        _ = GetService<WallpaperPickingService>().GetWallpaperAsync();
        _ = Host.StartAsync();

        Logger.LogInformation("正在初始化MainWindow。");
        MainWindow = GetService<MainWindow>();
        GetService<MainWindow>().Show();
    }

    private void OverrideFocusVisualStyle()
    {
        var overwriteList = new List<string>()
        {
            "MaterialDesignRaisedButton",
            "MaterialDesignFlatButton",
            "MaterialDesignFloatingActionMiniButton",
            "MaterialDesignPaperButton",
            "MaterialDesignCheckBox",
            "MaterialDesignUserForegroundCheckBox",
            "MaterialDesignComboBoxItemStyle",
            "MaterialDesignDataGridComboBoxItemStyle",
            "MaterialDesignDataGridComboBox",
            "MaterialDesignCardsListBoxItem",
            "MaterialDesignRadioButton",
            "MaterialDesignUserForegroundRadioButton",
            "MaterialDesignTabRadioButton",
            "MaterialDesignScrollBarButton",
            "MaterialDesignSnackbarActionButton",
            "MaterialDesignFilledUniformTabControl",
            "MaterialDesignSwitchToggleButton",
            "MaterialDesignSwitchAccentToggleButton"
        };
        var v = Resources[SystemParameters.FocusVisualStyleKey];
        foreach (var k in overwriteList)
        {
            var style = (Style?)TryFindResource(k);
            if (style == null) continue;
            Setter? targetSetter =
                DependencyPropertyHelper.FindSetter(style.Setters, FrameworkElement.FocusVisualStyleProperty);
            Setter? templateSetter = DependencyPropertyHelper.FindSetter(style.Setters, Control.TemplateProperty);

            if (targetSetter != null)
            {
                typeof(Setter).GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.SetValue(targetSetter, v);
            }

            if (templateSetter != null)
            {
                var template = (ControlTemplate)templateSetter.Value;
                foreach (var trigger in template.Triggers)
                {
                    if (trigger is not Trigger tt)
                    {
                        continue;
                    }

                    var target =
                        DependencyPropertyHelper.FindSetter(tt.Setters, FrameworkElement.FocusVisualStyleProperty);
                    if (target != null)
                    {
                        typeof(Setter).GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance)
                            ?.SetValue(target, v);
                    }
                }
            }
        }
    }

    private void BindingDiagnosticsOnBindingFailed(object? sender, BindingFailedEventArgs e)
    {
        if (e.EventType == TraceEventType.Verbose)
        {
            Logger?.LogTrace($"{e.Message}");
        }
        else
        {
            Logger?.LogWarning($"{e.Message}");
        }
    }

    public static void ReleaseLock()
    {
        var app = (App)Application.Current;
        app.Mutex?.ReleaseMutex();
    }
}
