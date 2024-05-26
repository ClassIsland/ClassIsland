using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Speech.Synthesis;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Diagnostics;
using System.Windows.Threading;

using ClassIsland.Controls;
using ClassIsland.Controls.AttachedSettingsControls;
using ClassIsland.Core;
using ClassIsland.Core.Abstraction.Services;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.Services.Logging;
using ClassIsland.Services.Management;
using ClassIsland.Services.MiniInfoProviders;
using ClassIsland.Services.NotificationProviders;
using ClassIsland.Services.SpeechService;
using ClassIsland.Views;

using MaterialDesignThemes.Wpf;

using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OfficeOpenXml;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using UpdateStatus = ClassIsland.Core.Enums.UpdateStatus;

namespace ClassIsland;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application, IAppHost
{
    private CrashWindow? CrashWindow;
    private Mutex? Mutex;
    private ILogger<App>? Logger { get; set; }
    //public static IHost? Host;

    public static readonly string AppDataFolderPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClassIsland");

    public static readonly string AppCacheFolderPath = "./Cache";

    public static T GetService<T>() => IAppHost.GetService<T>();

    public App()
    {
    }

    static App()
    {
        DependencyPropertyHelper.ForceOverwriteDependencyPropertyDefaultValue(ToolTipService.InitialShowDelayProperty,
            0);
        DependencyPropertyHelper.ForceOverwriteDependencyPropertyDefaultValue(ShadowAssist.CacheModeProperty,
            null);
    }

    public static ApplicationCommand ApplicationCommand
    {
        get;
        set;
    } = new();

    public Settings Settings { get; set; } = new();

    public static string AppVersion => Assembly.GetExecutingAssembly().GetName().Version!.ToString();

    public static string AppCodeName => "Firefly";

    public static string AppVersionLong =>
        $"{AppVersion}-{AppCodeName}-{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch}) (Core {IAppHost.CoreVersion})";

    private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        if (CrashWindow != null)
        {
            CrashWindow = null;
            GC.Collect();
        }
        Logger?.LogCritical(e.Exception, "发生严重错误。");
        //Settings.DiagnosticCrashCount++;
        //Settings.DiagnosticLastCrashTime = DateTime.Now;
        CrashWindow = new CrashWindow()
        {
            CrashInfo = e.Exception.ToString()
        };
#if DEBUG
        if (e.Exception.GetType() == typeof(ResourceReferenceKeyNotFoundException))
        {
            return;
        }
#endif
        Crashes.TrackError(e.Exception);
        CrashWindow.ShowDialog();
    }

    private async void App_OnStartup(object sender, StartupEventArgs e)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        //DependencyPropertyHelper.ForceOverwriteDependencyPropertyDefaultValue(FrameworkElement.FocusVisualStyleProperty,
        //    Resources[SystemParameters.FocusVisualStyleKey]);


        //ConsoleService.InitializeConsole();
        System.Windows.Forms.Application.EnableVisualStyles();
        DiagnosticService.BeginStartup();
        ConsoleService.InitializeConsole();

#if DEBUG
        AppCenter.LogLevel = Microsoft.AppCenter.LogLevel.Verbose;
#endif
        BindingDiagnostics.BindingFailed += BindingDiagnosticsOnBindingFailed;
        Crashes.SendingErrorReport += CrashesOnSendingErrorReport;
        AppCenter.Start("7039a2b0-8b4e-4d2d-8d2c-3c993ec26514", typeof(Analytics), typeof(Crashes));
        await AppCenter.SetEnabledAsync(false);

        var command = new RootCommand
        {
            new Option<string>(["--updateReplaceTarget", "-urt"], "更新时要替换的文件"),
            new Option<string>(["--updateDeleteTarget", "-udt"], "更新完成要删除的文件"),
            new Option<bool>(["--waitMutex", "-m"], "重复启动应用时，等待上一个实例退出而非直接退出应用。"),
            new Option<bool>(["--quiet", "-q"], "静默启动，启动时不显示Splash，并且启动后10秒内不显示任何通知。"),
            new Option<bool>(["-prevSessionMemoryKilled", "-psmk"], "上个会话因MLE结束。"),
            new Option<bool>(["-disableManagement", "-dm"], "在本次会话禁用集控。")
        };
        command.Handler = CommandHandler.Create((ApplicationCommand c) =>
        {
            ApplicationCommand = c;
        });
        await command.InvokeAsync(e.Args);

        // 检测Mutex
        Mutex = new Mutex(true, "ClassIsland.Lock", out var createNew);
        if (!createNew)
        {
            if (ApplicationCommand.WaitMutex)
            {
                try
                {
                    Mutex.WaitOne();
                }
                catch
                {
                    // ignored
                }
            }
            else
            {
                CommonDialog.ShowHint("ClassIsland已经在运行中，请勿重复启动第二个实例。\n\n要访问应用主菜单，请点击任务栏托盘中的应用图标。");
                Environment.Exit(0);
            }
        }

        // 检测临时目录
        if (Environment.CurrentDirectory.Contains(Path.GetTempPath()))
        {
            CommonDialog.ShowHint("ClassIsland正在临时目录下运行，应用设置、课表等数据很可能无法保存，或在应用退出后被自动删除。在使用本应用前，请务必将本应用解压到一个适合的位置。");
            Environment.Exit(0);
        }

        // 检测目录是否可以访问
        try
        {
            await File.WriteAllTextAsync("./.test-write", "");
            File.Delete("./.test-write");
        }
        catch (Exception ex)
        {
            CommonDialog.ShowError($"ClassIsland无法写入当前目录：{ex.Message}\n\n请将本软件解压到一个合适的位置后再运行。");
            Environment.Exit(0);
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

        FileFolderService.CreateFolders();
        bool isSystemSpeechSystemExist = false;
        IAppHost.Host = Microsoft.Extensions.Hosting.Host.
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
                services.AddSingleton<UpdateNodeSpeedTestingService>();
                services.AddSingleton<DiagnosticService>();
                services.AddSingleton<ManagementService>();
                services.AddSingleton<AppLogService>();
                services.AddSingleton<ILoggerProvider, AppLoggerProvider>();
                services.AddHostedService<MemoryWatchDogService>();
                services.AddSingleton<SpeechSynthesizer>(provider =>
                {
                    var s = new SpeechSynthesizer();
                    s.SetOutputToDefaultAudioDevice();
                    return s;
                });
                try // 检测SystemSpeechService是否存在
                {
                    var testSystemSpeechService = new SystemSpeechService();
                    isSystemSpeechSystemExist = true;
                }
                catch(Exception e)
                {
                    // ignore
                }
                services.AddSingleton<ISpeechService>((provider =>
                {
                    var s = provider.GetService<SettingsService>();
                    if (isSystemSpeechSystemExist)
                    {
                        return s?.Settings.SpeechSource switch
                        {
                            0 => new SystemSpeechService(),
                            1 => new EdgeTtsService(),
                            _ => new SystemSpeechService()
                        };
                    }
                    else
                    {
                        return new EdgeTtsService();
                    }
                    
                }));
                services.AddSingleton<ExactTimeService>();
                //services.AddSingleton(typeof(ApplicationCommand), ApplicationCommand);
                // Views
                services.AddSingleton<MainWindow>();
                services.AddSingleton<SplashWindow>();
                services.AddSingleton<ProfileSettingsWindow>();
                services.AddSingleton<HelpsWindow>();
                services.AddTransient<FeatureDebugWindow>();
                services.AddSingleton<TopmostEffectWindow>();
                services.AddSingleton<AppLogsWindow>();
                // 提醒提供方
                services.AddHostedService<ClassNotificationProvider>();
                services.AddHostedService<AfterSchoolNotificationProvider>();
                services.AddHostedService<WeatherNotificationProvider>();
                services.AddHostedService<ManagementNotificationProvider>();
                // 简略信息提供方
                services.AddHostedService<DateMiniInfoProvider>();
                services.AddHostedService<WeatherMiniInfoProvider>();
                services.AddHostedService<CountDownMiniInfoProvider>();
                // Transients
                services.AddTransient<ExcelImportWindow>();
                services.AddTransient<WallpaperPreviewWindow>();
                // Logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
#if DEBUG
                    builder.SetMinimumLevel(LogLevel.Trace);
#endif
                });
            }).Build();
        await GetService<ManagementService>().SetupManagement();
        await GetService<SettingsService>().LoadSettingsAsync();
        Settings = GetService<SettingsService>().Settings;
        Settings.IsSystemSpeechSystemExist = isSystemSpeechSystemExist;
        Settings.IsNetworkConnect = InternetGetConnectedState(out var _);
        Settings.DiagnosticStartupCount++;
        // 记录MLE
        if (ApplicationCommand.PrevSessionMemoryKilled)
        {
            Settings.DiagnosticMemoryKillCount++;
            Settings.DiagnosticLastMemoryKillTime = DateTime.Now;
        }
        ConsoleService.ConsoleVisible = Settings.IsDebugConsoleEnabled;
        //OverrideFocusVisualStyle();
        Logger = GetService<ILogger<App>>();
        Logger.LogInformation("初始化应用。");
        if (Settings.IsSplashEnabled && !ApplicationCommand.Quiet)
        {
            GetService<SplashWindow>().Show();
            GetService<SplashService>().CurrentProgress = 25;
            var b = false;
            while (!b)
            {
                Dispatcher.Invoke(DispatcherPriority.Background, () =>
                {
                    b = GetService<SplashWindow>().IsRendered;
                });
                //Console.WriteLine(b);
                await Dispatcher.Yield(DispatcherPriority.Background);
            }
        }
        GetService<SplashService>().CurrentProgress = 50;

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
            GetService<TaskBarIconService>().MainTaskBarIcon.ShowNotification("更新完成。", $"应用已更新到版本{AppVersion}。点击此处以查看更新日志。");
        }

        if (!ApplicationCommand.Quiet)  // 在静默启动时不进行更新相关操作
        {
            var r = await GetService<UpdateService>().AppStartup();
            if (r)
            {
                GetService<SplashService>().EndSplash();
                return;
            }
        }
        var attachedSettingsHostService = GetService<AttachedSettingsHostService>();
        attachedSettingsHostService.SubjectSettingsAttachedSettingsControls.Add(typeof(LessonControlAttachedSettingsControl));
        attachedSettingsHostService.ClassPlanSettingsAttachedSettingsControls.Add(typeof(LessonControlAttachedSettingsControl));
        attachedSettingsHostService.TimeLayoutSettingsAttachedSettingsControls.Add(typeof(LessonControlAttachedSettingsControl));
        attachedSettingsHostService.TimePointSettingsAttachedSettingsControls.Add(typeof(LessonControlAttachedSettingsControl));
        GetService<SplashService>().CurrentProgress = 75;

        await GetService<ProfileService>().LoadProfileAsync();
        GetService<WeatherService>();
        GetService<ExactTimeService>();
        _ = GetService<WallpaperPickingService>().GetWallpaperAsync();
        _ = IAppHost.Host.StartAsync();
        
        Logger.LogInformation("正在初始化MainWindow。");
        MainWindow = GetService<MainWindow>();
        GetService<MainWindow>().Show();
        GetService<SplashService>().CurrentProgress = 90;
    }

    private bool CategoryLevelFilter(string? arg1, LogLevel arg2)
    {
        
        return true;
    }

    private void CrashesOnSendingErrorReport(object sender, SendingErrorReportEventArgs e)
    {

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

    public static void Restart(bool quiet=false)
    {
        IAppHost.Host?.StopAsync(TimeSpan.FromSeconds(5));
        IAppHost.Host?.Services.GetService<SettingsService>()?.SaveSettings();
        IAppHost.Host?.Services.GetService<ProfileService>()?.SaveProfile();
        ReleaseLock();
        Current.Shutdown();
        var path = Environment.ProcessPath;
        var args = new List<string> { "-m" };
        if (quiet)
            args.Add("-q");
        if (path == null) 
            return;
        var replaced = path.Replace(".dll", ".exe");
        var startInfo = new ProcessStartInfo(replaced);
        foreach (var i in args)
        {
            startInfo.ArgumentList.Add(i);
        }
        Process.Start(startInfo);
    }
}
