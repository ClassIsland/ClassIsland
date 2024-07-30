using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Speech.Synthesis;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Diagnostics;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using ClassIsland.Controls;
using ClassIsland.Controls.AttachedSettingsControls;
using ClassIsland.Controls.Components;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Commands;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Controls.CommonDialog;
using ClassIsland.Core.Extensions;
using ClassIsland.Core.Extensions.Registry;
using ClassIsland.Shared;
using ClassIsland.Shared.Abstraction.Services;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.Services.AppUpdating;
using ClassIsland.Services.Logging;
using ClassIsland.Services.Management;
using ClassIsland.Services.MiniInfoProviders;
using ClassIsland.Services.NotificationProviders;
using ClassIsland.Services.SpeechService;
using ClassIsland.Views;
using ClassIsland.Views.SettingPages;
using MaterialDesignThemes.Wpf;

using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OfficeOpenXml;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using UpdateStatus = ClassIsland.Shared.Enums.UpdateStatus;
#if DEBUG
using JetBrains.Profiler.Api;
#endif
using System.Xml.Linq;
using ClassIsland.Core;
using ClassIsland.Services.Grpc;
using ClassIsland.Shared.IPC;
using ClassIsland.Shared.IPC.Protobuf.Client;
using GrpcDotNetNamedPipes;
using Sentry;

namespace ClassIsland;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : AppBase, IAppHost
{
    private CrashWindow? CrashWindow;
    public Mutex? Mutex { get; set; }
    public bool IsMutexCreateNew { get; set; } = false;
    private ILogger<App>? Logger { get; set; }
    //public static IHost? Host;

    public static readonly string AppDataFolderPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClassIsland");

    public static readonly string AppConfigPath = "./Config";

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

    public static string AppCodeName => "Griseo";

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
        SentrySdk.CaptureException(e.Exception, scope =>
        {
            scope.Level = SentryLevel.Fatal;    
        });
        CrashWindow.ShowDialog();
    }

    private async void App_OnStartup(object sender, StartupEventArgs e)
    {
        var transaction = SentrySdk.StartTransaction(
            "startup",
            "startup"
        );
        SentrySdk.ConfigureScope(s => s.Transaction = transaction);
        var spanPreInit = transaction.StartChild("startup-init");
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        //DependencyPropertyHelper.ForceOverwriteDependencyPropertyDefaultValue(FrameworkElement.FocusVisualStyleProperty,
        //    Resources[SystemParameters.FocusVisualStyleKey]);
        Environment.CurrentDirectory = System.Windows.Forms.Application.StartupPath;

        //ConsoleService.InitializeConsole();
        System.Windows.Forms.Application.EnableVisualStyles();
        DiagnosticService.BeginStartup();
        ConsoleService.InitializeConsole();

#if DEBUG
        AppCenter.LogLevel = Microsoft.AppCenter.LogLevel.Verbose;
#endif
        BindingDiagnostics.BindingFailed += BindingDiagnosticsOnBindingFailed;

        // 检测Mutex
        if (!IsMutexCreateNew)
        {
            if (!ApplicationCommand.WaitMutex)
            {
                spanPreInit.Finish();
                transaction.Finish();
                ProcessInstanceExisted();
                Environment.Exit(0);
            }
        }

        // 检测临时目录
        if (Environment.CurrentDirectory.Contains(Path.GetTempPath()))
        {
            CommonDialog.ShowHint("ClassIsland正在临时目录下运行，应用设置、课表等数据很可能无法保存，或在应用退出后被自动删除。在使用本应用前，请务必将本应用解压到一个适合的位置。");
            Environment.Exit(0);
        }

        // 检测桌面文件夹
        if (!ApplicationCommand.Quiet &&
            Settings.DirectoryIsDesktopShowed != Environment.UserName &&
            Environment.CurrentDirectory == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
        {
            Settings.DirectoryIsDesktopShowed = Environment.UserName;
            DirectoryIsDesktop();
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

        // 检测 DWM
        DwmIsCompositionEnabled(out var isDwmEnabled);
        if (!isDwmEnabled)
        {
            CommonDialog.ShowError("运行ClassIsland需要开启Aero效果。请在【控制面板】->【个性化】中启用Aero主题，然后再尝试运行ClassIsland。");
            Environment.Exit(0);
        }
        var spanProcessUpdate = spanPreInit.StartChild("startup-process-update");

        if (ApplicationCommand.UpdateReplaceTarget != null)
        {
            //MessageBox.Show($"Update replace {ApplicationCommand.UpdateReplaceTarget}");
            UpdateService.ReplaceApplicationFile(ApplicationCommand.UpdateReplaceTarget);
            Process.Start(new ProcessStartInfo()
            {
                FileName = ApplicationCommand.UpdateReplaceTarget,
                ArgumentList = { "-udt", Environment.ProcessPath!, "-m", "true" }
            });
            Restart();
            return;
        }
        if (ApplicationCommand.UpdateDeleteTarget != null)
        {            
            //MessageBox.Show($"Update DELETE {ApplicationCommand.UpdateDeleteTarget}");
            UpdateService.RemoveUpdateTemporary(ApplicationCommand.UpdateDeleteTarget);
        }
        spanProcessUpdate.Finish();

        FileFolderService.CreateFolders();
        PluginService.ProcessPluginsInstall();
        bool isSystemSpeechSystemExist = false;
        var spanHostBuilding = spanPreInit.StartChild("startup-host-building");
        IAppHost.Host = Microsoft.Extensions.Hosting.Host.
            CreateDefaultBuilder().
            UseContentRoot(AppContext.BaseDirectory).
            ConfigureServices((context, services) =>
            {
                services.AddSingleton<SettingsService>();
                services.AddSingleton<UpdateService>();
                services.AddSingleton<ITaskBarIconService, TaskBarIconService>();
                services.AddSingleton<WallpaperPickingService>();
                services.AddSingleton<INotificationHostService, NotificationHostService>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<MiniInfoProviderHostService>();
                services.AddSingleton<IWeatherService, WeatherService>();
                services.AddSingleton<FileFolderService>();
                services.AddSingleton<IAttachedSettingsHostService, AttachedSettingsHostService>();
                services.AddSingleton<IProfileService, ProfileService>();
                services.AddSingleton<ISplashService, SplashService>();
                services.AddSingleton<IHangService, HangService>();
                services.AddSingleton<ConsoleService>();
                //services.AddHostedService<BootService>();
                services.AddSingleton<UpdateNodeSpeedTestingService>();
                services.AddSingleton<DiagnosticService>();
                services.AddSingleton<IManagementService, ManagementService>();
                services.AddSingleton<AppLogService>();
                services.AddSingleton<ILoggerProvider, SentryLoggerProvider>();
                services.AddSingleton<ILoggerProvider, AppLoggerProvider>();
                services.AddSingleton<IComponentsService, ComponentsService>();
                services.AddSingleton<ILessonsService, LessonsService>();
                services.AddSingleton<IUriNavigationService, UriNavigationService>();
                services.AddHostedService<MemoryWatchDogService>();
                services.AddSingleton(new NamedPipeServer(IpcClient.PipeName));
                services.AddSingleton<IPluginService, PluginService>();
                services.AddSingleton<IPluginMarketService, PluginMarketService>();
                try // 检测SystemSpeechService是否存在
                {
                    _ = new SpeechSynthesizer();
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
                services.AddSingleton<IExactTimeService, ExactTimeService>();
                //services.AddSingleton(typeof(ApplicationCommand), ApplicationCommand);
                // Views
                services.AddSingleton<MainWindow>();
                services.AddSingleton<SplashWindow>();
                services.AddSingleton<HelpsWindow>();
                services.AddTransient<FeatureDebugWindow>();
                services.AddSingleton<TopmostEffectWindow>();
                services.AddSingleton<AppLogsWindow>();
                services.AddSingleton<SettingsWindowNew>();
                services.AddSingleton<ProfileSettingsWindow>((s) => new ProfileSettingsWindow()
                {
                    MainViewModel = s.GetService<MainWindow>()?.ViewModel ?? new()
                });
                // 设置页面
                services.AddSettingsPage<GeneralSettingsPage>();
                services.AddSettingsPage<ComponentsSettingsPage>();
                services.AddSettingsPage<AppearanceSettingsPage>();
                services.AddSettingsPage<NotificationSettingsPage>();
                services.AddSettingsPage<WindowSettingsPage>();
                services.AddSettingsPage<WeatherSettingsPage>();
                services.AddSettingsPage<UpdatesSettingsPage>();
                services.AddSettingsPage<PrivacySettingsPage>();
                services.AddSettingsPage<PluginsSettingsPage>();
                services.AddSettingsPage<TestSettingsPage>();
                services.AddSettingsPage<DebugPage>();
                services.AddSettingsPage<DebugBrushesSettingsPage>();
                services.AddSettingsPage<AboutSettingsPage>();
                // 主界面组件
                services.AddComponent<TextComponent, TextComponentSettingsControl>();
                services.AddComponent<LegacyScheduleComponent>();
                services.AddComponent<ScheduleComponent>();
                services.AddComponent<DateComponent>();
                services.AddComponent<ClockComponent, ClockComponentSettingsControl>();
                services.AddComponent<WeatherComponent, WeatherComponentSettingsControl>();
                services.AddComponent<CountDownComponent, CountDownComponentSettingsControl>();
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
                    builder.AddSentry(o =>
                    {
                        o.InitializeSdk = false;
                        o.MinimumBreadcrumbLevel = LogLevel.Information;
                    });
                    // TODO: 添加写入本地log文件
#if DEBUG
                    builder.SetMinimumLevel(LogLevel.Trace);
#endif
                });
                // Grpc
                services.AddGrpcService<RemoteUriNavigationService>();
                // Plugins
                PluginService.InitializePlugins(context, services);
            }).Build();
#if DEBUG
        MemoryProfiler.GetSnapshot("Host built");
#endif
        spanHostBuilding.Finish();
        spanPreInit.Finish();
        var spanLaunching = transaction.StartChild("startup-launching");
        CommandManager.RegisterClassCommandBinding(typeof(Window), new CommandBinding(UriNavigationCommands.UriNavigationCommand, UriNavigationCommandExecuted));
        CommandManager.RegisterClassCommandBinding(typeof(Page), new CommandBinding(UriNavigationCommands.UriNavigationCommand, UriNavigationCommandExecuted));
        var spanSetupMgmt = spanLaunching.StartChild("startup-setup-mgmt");
        await GetService<IManagementService>().SetupManagement();
        spanSetupMgmt.Finish();
        var spanLoadingSettings = spanLaunching.StartChild("startup-loading-settings");
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
        spanLoadingSettings.Finish();
        //OverrideFocusVisualStyle();
        Logger = GetService<ILogger<App>>();
        Logger.LogInformation("初始化应用。");

        if (Settings.IsSplashEnabled && !ApplicationCommand.Quiet)
        {
            var spanShowSplash = spanLaunching.StartChild("startup-show-splash");
            GetService<SplashWindow>().Show();
            GetService<ISplashService>().CurrentProgress = 25;
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
            spanShowSplash.Finish();
        }
        GetService<ISplashService>().CurrentProgress = 50;

        var spanStartHangService = spanLaunching.StartChild("startup-start-hang-service");
        GetService<IHangService>();
        spanStartHangService.Finish();

        var spanCreateTaskbarIcon = spanLaunching.StartChild("startup-create-taskbar-icon");
        try
        {
            GetService<ITaskBarIconService>().MainTaskBarIcon.ForceCreate(false);
            spanCreateTaskbarIcon.Finish();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "创建任务栏图标失败。");
            spanCreateTaskbarIcon.Finish(ex);
        }

        if (ApplicationCommand.UpdateDeleteTarget != null)
        {
            GetService<SettingsService>().Settings.LastUpdateStatus = UpdateStatus.UpToDate;
            GetService<ITaskBarIconService>().MainTaskBarIcon.ShowNotification("更新完成。", $"应用已更新到版本{AppVersion}。点击此处以查看更新日志。");
        }

        if (!ApplicationCommand.Quiet)  // 在静默启动时不进行更新相关操作
        {
            var spanCheckUpdate = spanLaunching.StartChild("startup-process-update");
            var r = await GetService<UpdateService>().AppStartup();
            spanCheckUpdate.Finish();
            if (r)
            {
                GetService<ISplashService>().EndSplash();
                return;
            }
        }
        var attachedSettingsHostService = GetService<IAttachedSettingsHostService>();
        attachedSettingsHostService.SubjectSettingsAttachedSettingsControls.Add(typeof(LessonControlAttachedSettingsControl));
        attachedSettingsHostService.ClassPlanSettingsAttachedSettingsControls.Add(typeof(LessonControlAttachedSettingsControl));
        attachedSettingsHostService.TimeLayoutSettingsAttachedSettingsControls.Add(typeof(LessonControlAttachedSettingsControl));
        attachedSettingsHostService.TimePointSettingsAttachedSettingsControls.Add(typeof(LessonControlAttachedSettingsControl));
        GetService<ISplashService>().CurrentProgress = 75;

        await GetService<IProfileService>().LoadProfileAsync();
        GetService<IWeatherService>();
        GetService<IExactTimeService>();
        _ = GetService<WallpaperPickingService>().GetWallpaperAsync();
        _ = IAppHost.Host.StartAsync();

        var spanLoadMainWindow = spanLaunching.StartChild("span-loading-mainWindow");
        Logger.LogInformation("正在初始化MainWindow。");
#if DEBUG
        MemoryProfiler.GetSnapshot("Pre MainWindow init");
#endif
        var mw = GetService<MainWindow>();
        MainWindow = mw;
        mw.StartupCompleted += (o, args) =>
        {
            spanLoadMainWindow.Finish();
            transaction.Finish();
            SentrySdk.ConfigureScope(s => s.Transaction = null);
        };
#if DEBUG
        MemoryProfiler.GetSnapshot("Pre MainWindow show");
#endif
        GetService<MainWindow>().Show();
        GetService<ISplashService>().CurrentProgress = 90;

        // 注册uri导航
        var uriNavigationService = GetService<IUriNavigationService>();
        uriNavigationService.HandleAppNavigation("test", args => CommonDialog.ShowInfo($"测试导航：{args.Uri}"));
        uriNavigationService.HandleAppNavigation("settings", args => GetService<SettingsWindowNew>().OpenUri(args.Uri));
        uriNavigationService.HandleAppNavigation("profile", args => GetService<MainWindow>().OpenProfileSettingsWindow());
        uriNavigationService.HandleAppNavigation("helps", args => GetService<MainWindow>().OpenHelpsWindow());

        IAppHost.Host.BindGrpcServices();
        GetService<NamedPipeServer>().Start();
    }

    private void UriNavigationCommandExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        var uri = "";
        if (e.Parameter is string uriRaw)
        {
            uri = uriRaw;
        }

        //if (sender is Hyperlink hyperlink)
        //{
        //    uri = hyperlink.GetHref();
        //}
        try
        {
            IAppHost.GetService<IUriNavigationService>().NavigateWrapped(new Uri(uri));
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "无法导航到 {}", uri);
            CommonDialog.ShowError($"无法导航到 {uri}：{ex.Message}");
        }
    }

    private void ProcessInstanceExisted()
    {
        var r = new CommonDialogBuilder()
            .SetContent("ClassIsland已经在运行中，请勿重复启动第二个实例。\n\n要访问应用主菜单，请点击任务栏托盘中的应用图标。")
            .SetBitmapIcon(new Uri("/Assets/HoYoStickers/帕姆_注意.png", UriKind.RelativeOrAbsolute))
            .AddAction("退出应用", PackIconKind.ExitToApp)
            .AddAction("重启现有实例", PackIconKind.Restart, true)
            .ShowDialog();
        if (r != 1)
        {
            return;
        }

        try
        {
            var proc = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Environment.ProcessPath)).Where(x=>x.Id != Environment.ProcessId);
            foreach (var i in proc)
            {
                i.Kill(true);
            }

            Process.Start(new ProcessStartInfo(Environment.ProcessPath ?? "")
            {
                ArgumentList = { "-m" }
            });
        }
        catch (Exception e)
        {
            CommonDialog.ShowError($"无法重新启动应用，可能当前运行的实例正在以管理员身份运行。请使用任务管理器终止正在运行的实例，然后再试一次。\n\n{e.Message}");

        }
    }

    public static void DirectoryIsDesktop(bool debug = false)
    {
        var r = new CommonDialogBuilder().SetContent("ClassIsland正在桌面上运行，应用设置、课表等数据将会直接存放到桌面上。在使用本应用前，请将本应用移动到一个适合的位置。")
                                         .SetBitmapIcon(new Uri("/Assets/HoYoStickers/帕姆_注意.png", UriKind.RelativeOrAbsolute))
                                         .AddAction("退出应用", PackIconKind.ExitToApp);
        var destinationDirs = new List<string>(4);
        if (debug)
        {
            destinationDirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
            destinationDirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        }
        foreach (var drive in DriveInfo.GetDrives().Where(x => x.DriveType == DriveType.Fixed && !x.Name.Contains(Environment.GetEnvironmentVariable("systemdrive")!)))
            try
            {
                File.WriteAllTextAsync(Path.Combine(drive.Name, ".test-write"), "");
                File.Delete(Path.Combine(drive.Name, ".test-write"));
                destinationDirs.Add(drive.Name);
            } catch (Exception _)
            { // ignored
            }
        foreach (var dir in destinationDirs)
            r.AddAction($"移动到 {dir.ToFriendlyPath()}", PackIconKind.FolderMoveOutline, isPrimary: dir == @"D:\");
        var c = r.ShowDialog();
        if (c == 0)
        {
            if (!debug)
                Environment.Exit(0);
        } else if (c > 0)
        {
            var destinationDir = Path.Combine(destinationDirs[c - 1], "ClassIsland");
            try
            {
                IAppHost.Host?.Services.GetService<ILessonsService>()?.StopMainTimer();
                IAppHost.Host?.Services.GetService<NamedPipeServer>()?.Kill();
                IAppHost.Host?.StopAsync(TimeSpan.FromSeconds(5));
                IAppHost.Host?.Services.GetService<SettingsService>()?.SaveSettings();
                IAppHost.Host?.Services.GetService<IProfileService>()?.SaveProfile();
                ReleaseLock();
                var args = new List<string> {"-m", "-udt", Environment.ProcessPath!.Replace(".dll", ".exe")};
                foreach (var i in new[]
                         {
                             Environment.ProcessPath.Replace(".dll", ".exe"),
                             "Settings.json",
                             "Settings.json.bak",
                             @".\Profiles",
                             @".\Config",
                             @".\Temp",
                             @".\Cache"
                         })
                    FileFolderService.Move(i, destinationDir, ref args);
                new CommonDialogBuilder().SetContent($"已将 ClassIsland 移动到 {destinationDir.ToFriendlyPath()}，桌面上如有遗留文件需要自行删除。")
                                         .SetBitmapIcon(new Uri("/Assets/HoYoStickers/帕姆_点赞.png", UriKind.RelativeOrAbsolute))
                                         .AddConfirmAction()
                                         .ShowDialog();
                Current.Shutdown();
                var startInfo = new ProcessStartInfo(Path.Combine(destinationDir, new FileInfo(Environment.ProcessPath).Name.Replace(".dll", ".exe")));
                foreach (var i in args)
                    startInfo.ArgumentList.Add(i);
                Process.Start(startInfo);
            } catch (Exception ex)
            {
                Console.WriteLine($"无法将 ClassIsland 移动到 {destinationDir}，\n"
                                + $"错误原因：{ex}");
                new CommonDialogBuilder().SetContent($"无法将 ClassIsland 移动到 {destinationDir.ToFriendlyPath()}，请手动将本应用移动到一个专属的文件夹后再运行。\n\n"
                                                   + $"错误原因：{ex.Message}")
                                         .SetBitmapIcon(new Uri("/Assets/HoYoStickers/帕姆_不可以.png", UriKind.RelativeOrAbsolute))
                                         .AddConfirmAction()
                                         .ShowDialog();
                if (!debug)
                    Environment.Exit(0);
            }
        }
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

    public override void Stop()
    {
        IAppHost.Host?.Services.GetService<ILessonsService>()?.StopMainTimer();
        IAppHost.Host?.Services.GetService<NamedPipeServer>()?.Kill();
        IAppHost.Host?.StopAsync(TimeSpan.FromSeconds(5));
        IAppHost.Host?.Services.GetService<SettingsService>()?.SaveSettings();
        IAppHost.Host?.Services.GetService<IProfileService>()?.SaveProfile();
        ReleaseLock();
        Current.Shutdown();
    }

    public override void Restart(bool quiet=false)
    {
        Stop();
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
