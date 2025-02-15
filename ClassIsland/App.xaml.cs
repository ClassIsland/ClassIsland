using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Diagnostics;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
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
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Models.Ruleset;
using ClassIsland.Shared.IPC;
using Sentry;
using ClassIsland.Core.Controls.Ruleset;
using ClassIsland.Models.Rules;
using ClassIsland.Models.Actions;
using ClassIsland.Controls.RuleSettingsControls;
using ClassIsland.Shared.IPC.Abstractions.Services;
using dotnetCampus.Ipc.CompilerServices.GeneratedProxies;
using ControlzEx.Native;
using ClassIsland.Controls.ActionSettingsControls;
using ClassIsland.Controls.AuthorizeProvider;
using ClassIsland.Core.Enums;
using ClassIsland.Services.ActionHandlers;
using System.Diagnostics.Tracing;
#if IsMsix
using Windows.ApplicationModel;
using Windows.Storage;
#endif
using ClassIsland.Services.Automation.Triggers;
using ClassIsland.Controls.TriggerSettingsControls;
using ClassIsland.Models.Automation.Triggers;
using MahApps.Metro.Controls;
using Walterlv.Threading;
using Walterlv.Windows;

namespace ClassIsland;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : AppBase, IAppHost
{
    public static bool IsAssetsTrimmedInternal { get; } =
#if TrimAssets 
        true;
#else
        false;
#endif
    
    private CrashWindow? CrashWindow;
    public Mutex? Mutex { get; set; }
    public bool IsMutexCreateNew { get; set; } = false;
    private ILogger<App>? Logger { get; set; }
    //public static IHost? Host;

    public static readonly string AppRootFolderPath =
#if IsMsix
        ApplicationData.Current.LocalFolder.Path;
#else
        "./";
#endif
    public static readonly string AppDataFolderPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClassIsland");

    public static readonly string AppLogFolderPath = Path.Combine(AppRootFolderPath, "Logs");

    public static readonly string AppConfigPath = Path.Combine(AppRootFolderPath, "Config");

    public static readonly string AppCacheFolderPath =
#if IsMsix
        ApplicationData.Current.LocalCacheFolder.Path;
#else
        Path.Combine(AppRootFolderPath, "Cache");
#endif

    public static readonly string AppTempFolderPath =
#if IsMsix
        ApplicationData.Current.TemporaryFolder.Path;
#else
        Path.Combine(AppRootFolderPath, "Temp");
#endif

    public static T GetService<T>() => IAppHost.GetService<T>();

    public bool IsSentryEnabled { get; set; } = false;

    private bool _isStartedCompleted = false;

    internal static bool _isCriticalSafeModeEnabled = false;

    public override bool IsDevelopmentBuild =>
#if DevelopmentBuild
        true
#else
        false
#endif
    ;

    public override bool IsMsix => _isMsix;

    public static readonly bool _isMsix =
#if IsMsix
        true
#else
        false
#endif
    ;

    public App()
    {
        //AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.EnablePointerSupport", true);
        //TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
    }

    static App()
    {
        DependencyPropertyHelper.ForceOverwriteDependencyPropertyDefaultValue(ToolTipService.InitialShowDelayProperty,
            0);
        DependencyPropertyHelper.ForceOverwriteDependencyPropertyDefaultValue(ShadowAssist.CacheModeProperty,
            null);
    }

    private void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        Dispatcher.Invoke(() =>
        {
            ProcessUnhandledException(e.Exception);
        });
    }

    public static ApplicationCommand ApplicationCommand
    {
        get;
        set;
    } = new();

    public Settings Settings { get; set; } = new();

    private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            ProcessUnhandledException(e.Exception);
        }
        catch
        {
            Exit += (_, _) =>
            {
                DiagnosticService.ProcessCriticalException(e.Exception);
            };
            Stop();
        }
        e.Handled = true;
    }

    internal void ProcessUnhandledException(Exception e, bool critical=false)
    {
#if DEBUG
        if (e.GetType() == typeof(ResourceReferenceKeyNotFoundException))
        {
            return;
        }
#endif

        var safe = _isCriticalSafeModeEnabled && (!(IAppHost.TryGetService<IWindowRuleService>()?.IsForegroundWindowClassIsland() ?? false));
        if (safe)
        {
            Logger?.LogCritical(e, "发生严重错误（应用被教学安全模式退出）");
            Task.Run(async () =>
            {
                await Task.Delay(4000);
                AppBase.Current.Stop();
                Application.Current.Shutdown();
            });
        }
        else
        {
            Logger?.LogCritical(e, "发生严重错误。");
        }

        // wtf ↓
        //if (CrashWindow != null)
        //{
        //    CrashWindow = null;
        //    GC.Collect();
        //}

        //Settings.DiagnosticCrashCount++;
        //Settings.DiagnosticLastCrashTime = DateTime.Now;
        CrashWindow = new CrashWindow()
        {
            CrashInfo = e.ToString(),
            AllowIgnore = _isStartedCompleted && !critical,
            IsCritical = critical
        };
        if (!critical)  // 全局未捕获的异常应该由 SentrySdk 自行捕获。
        {
            SentrySdk.CaptureException(e, scope =>
            {
                scope.Level = SentryLevel.Fatal;
            });
        }
        if (!safe)
            CrashWindow.ShowDialog();
        else
        {
            AppBase.Current.Stop();
            Application.Current.Shutdown();
        }
    }

    private async void App_OnStartup(object sender, StartupEventArgs e)
    {
        var transaction = SentrySdk.StartTransaction(
            "startup",
            "startup"
        );
        SentrySdk.ConfigureScope(s => s.Transaction = transaction);
        var spanPreInit = transaction.StartChild("startup-init");
        MyWindow.ShowOssWatermark = ApplicationCommand.ShowOssWatermark;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        //DependencyPropertyHelper.ForceOverwriteDependencyPropertyDefaultValue(FrameworkElement.FocusVisualStyleProperty,
        //    Resources[SystemParameters.FocusVisualStyleKey]);
        Environment.CurrentDirectory = System.Windows.Forms.Application.StartupPath;

        //ConsoleService.InitializeConsole();
        System.Windows.Forms.Application.EnableVisualStyles();
        DiagnosticService.BeginStartup();
        ConsoleService.InitializeConsole();
        //if (IsAssetsTrimmed())
        //{
        //    Resources["HarmonyOsSans"] = FindResource("BackendFontFamily");
        //}

        BindingDiagnostics.BindingFailed += BindingDiagnosticsOnBindingFailed;

        Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");
        Thread.CurrentThread.CurrentCulture = new CultureInfo("zh-CN");
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.IetfLanguageTag)));

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
        if (Environment.CurrentDirectory == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) && !Settings.IsWelcomeWindowShowed)
        {
            var r = CommonDialog.ShowHint("ClassIsland正在桌面上运行，应用设置、课表等数据将会直接存放到桌面上。在使用本应用前，请将本应用移动到一个单独的文件夹中。");
            if (r == 0)
                Environment.Exit(0);
        }

        // 检测目录是否可以访问
        try
        {
            var testWritePath = Path.Combine(AppRootFolderPath, "./.test-write");
            await File.WriteAllTextAsync(testWritePath, "");
            File.Delete(testWritePath);
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
            await UpdateService.ReplaceApplicationFile(ApplicationCommand.UpdateReplaceTarget);
            Process.Start(new ProcessStartInfo()
            {
                FileName = ApplicationCommand.UpdateReplaceTarget,
                ArgumentList = { "-udt", Environment.ProcessPath!, "-m", "true" }
            });
            Stop();
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
                services.AddSingleton<IComponentsService, ComponentsService>();
                services.AddSingleton<ILessonsService, LessonsService>();
                services.AddSingleton<IUriNavigationService, UriNavigationService>();
                services.AddHostedService<MemoryWatchDogService>();
                services.AddSingleton<IPluginService, PluginService>();
                services.AddSingleton<IPluginMarketService, PluginMarketService>();
                services.AddSingleton<IRulesetService, RulesetService>();
                services.AddSingleton<IActionService, ActionService>();
                services.AddSingleton<IWindowRuleService, WindowRuleService>();
                services.AddSingleton<IAutomationService, AutomationService>();
                try // 检测SystemSpeechService是否存在
                {
                    var s = new SpeechSynthesizer();
                    s.SetOutputToDefaultAudioDevice();
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
                            2 => new GptSoVitsService(),
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
                services.AddSingleton<IProfileAnalyzeService, ProfileAnalyzeService>();
                services.AddSingleton<IIpcService, IpcService>();
                services.AddSingleton<IAuthorizeService, AuthorizeService>();
                services.AddSingleton<UriTriggerHandlerService>();
                services.AddSingleton<SignalTriggerHandlerService>();
                // Views
                services.AddSingleton<MainWindow>();
                services.AddSingleton<SplashWindow>();
                services.AddTransient<FeatureDebugWindow>();
                services.AddSingleton<TopmostEffectWindow>(BuildTopmostEffectWindow);
                services.AddSingleton<AppLogsWindow>();
                services.AddSingleton<SettingsWindowNew>();
                services.AddSingleton<ProfileSettingsWindow>((s) => new ProfileSettingsWindow()
                {
                    MainViewModel = s.GetService<MainWindow>()?.ViewModel ?? new()
                });
                services.AddTransient<ClassPlanDetailsWindow>();
                services.AddTransient<WindowRuleDebugWindow>();
                // 设置页面
                services.AddSettingsPage<GeneralSettingsPage>();
                services.AddSettingsPage<ComponentsSettingsPage>();
                services.AddSettingsPage<AppearanceSettingsPage>();
                services.AddSettingsPage<NotificationSettingsPage>();
                services.AddSettingsPage<WindowSettingsPage>();
                services.AddSettingsPage<WeatherSettingsPage>();
                services.AddSettingsPage<UpdatesSettingsPage>();
                services.AddSettingsPage<AutomationSettingsPage>();
                services.AddSettingsPage<StorageSettingsPage>();
                services.AddSettingsPage<PrivacySettingsPage>();
                services.AddSettingsPage<PluginsSettingsPage>();
                services.AddSettingsPage<TestSettingsPage>();
                services.AddSettingsPage<DebugPage>();
                services.AddSettingsPage<DebugBrushesSettingsPage>();
                services.AddSettingsPage<AboutSettingsPage>();
                services.AddSettingsPage<ManagementSettingsPage>();
                services.AddSettingsPage<ManagementCredentialsSettingsPage>();
                services.AddSettingsPage<ManagementPolicySettingsPage>();
                // 主界面组件
                services.AddComponent<TextComponent, TextComponentSettingsControl>();
                services.AddComponent<SeparatorComponent>();
                services.AddComponent<ScheduleComponent, ScheduleComponentSettingsControl>();
                services.AddComponent<DateComponent>();
                services.AddComponent<ClockComponent, ClockComponentSettingsControl>();
                services.AddComponent<WeatherComponent, WeatherComponentSettingsControl>();
                services.AddComponent<CountDownComponent, CountDownComponentSettingsControl>();
                services.AddComponent<SlideComponent, SlideComponentSettingsControl>();
                services.AddComponent<GroupComponent>();
                // 提醒提供方
                services.AddHostedService<ClassNotificationProvider>();
                services.AddHostedService<AfterSchoolNotificationProvider>();
                services.AddHostedService<WeatherNotificationProvider>();
                services.AddHostedService<ManagementNotificationProvider>();
                services.AddHostedService<ActionNotificationProvider>();
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
                    var debug = false;
#if DEBUG
                    debug = true;
#endif
                    if (ApplicationCommand.Verbose || debug)
                    {
                        builder.SetMinimumLevel(LogLevel.Trace);
                    }
                });
                services.AddSingleton<ILoggerProvider, SentryLoggerProvider>();
                services.AddSingleton<ILoggerProvider, AppLoggerProvider>();
                services.AddSingleton<ILoggerProvider, FileLoggerProvider>();
                // AttachedSettings
                services.AddAttachedSettingsControl<AfterSchoolNotificationAttachedSettingsControl>();
                services.AddAttachedSettingsControl<ClassNotificationAttachedSettingsControl>();
                services.AddAttachedSettingsControl<LessonControlAttachedSettingsControl>();
                services.AddAttachedSettingsControl<WeatherNotificationAttachedSettingsControl>();
                // 触发器
                services.AddTrigger<SignalTrigger, SignalTriggerSettingsControl>();
                services.AddTrigger<UriTrigger, UriTriggerSettingsControl>();
                services.AddTrigger<RulesetChangedTrigger>();
                services.AddTrigger<CronTrigger, CronTriggerSettingsControl>();
                services.AddTrigger<AppStartupTrigger>();
                services.AddTrigger<AppStoppingTrigger>();
                services.AddTrigger<OnClassTrigger>();
                services.AddTrigger<OnBreakingTimeTrigger>();
                services.AddTrigger<OnAfterSchoolTrigger>();
                services.AddTrigger<CurrentTimeStateChangedTrigger>();
                // 规则
                services.AddRule("classisland.test.true", "总是为真", onHandle: _ => true);
                services.AddRule("classisland.test.false", "总是为假", onHandle: _ => false);
                services.AddRule<StringMatchingSettings, RulesetStringMatchingSettingsControl>("classisland.windows.className", "前台窗口类名", PackIconKind.WindowMaximize);
                services.AddRule<StringMatchingSettings, RulesetStringMatchingSettingsControl>("classisland.windows.text", "前台窗口标题", PackIconKind.FormatTitle);
                services.AddRule<WindowStatusRuleSettings, WindowStatusRuleSettingsControl>("classisland.windows.status", "前台窗口状态是", PackIconKind.DockWindow);
                services.AddRule<StringMatchingSettings, RulesetStringMatchingSettingsControl>("classisland.windows.processName", "前台窗口进程", PackIconKind.ApplicationCogOutline);
                services.AddRule<CurrentSubjectRuleSettings, CurrentSubjectRuleSettingsControl>("classisland.lessons.currentSubject", "科目是", PackIconKind.BookOutline);
                services.AddRule<CurrentSubjectRuleSettings, CurrentSubjectRuleSettingsControl>("classisland.lessons.nextSubject", "下节课科目是", PackIconKind.BookArrowRightOutline);
                services.AddRule<CurrentSubjectRuleSettings, CurrentSubjectRuleSettingsControl>("classisland.lessons.previousSubject", "上节课科目是", PackIconKind.BookArrowLeftOutline);
                services.AddRule<TimeStateRuleSettings, TimeStateRuleSettingsControl>("classisland.lessons.timeState", "当前时间状态是", PackIconKind.ClockOutline);
                services.AddRule<CurrentWeatherRuleSettings, CurrentWeatherRuleSettingsControl>("classisland.weather.currentWeather", "当前天气是", PackIconKind.WeatherCloudy);
                services.AddRule<StringMatchingSettings, RulesetStringMatchingSettingsControl>("classisland.weather.hasWeatherAlert", "存在气象预警", PackIconKind.WeatherCloudyAlert);
                services.AddRule<RainTimeRuleSettings, RainTimeRuleSettingsControl>("classisland.weather.rainTime", "距离降水开始/结束还剩", PackIconKind.WeatherHeavyRain);
                // 行动
                services.AddAction<SignalTriggerSettings, BroadcastSignalActionSettingsControl>("classisland.broadcastSignal", "广播信号", PackIconKind.Broadcast);
                services.AddAction<CurrentComponentConfigActionSettings, CurrentComponentConfigActionSettingsControl>("classisland.settings.currentComponentConfig", "组件配置方案", PackIconKind.WidgetsOutline);
                services.AddAction<ThemeActionSettings, ThemeActionSettingsControl>("classisland.settings.theme", "应用主题", PackIconKind.ThemeLightDark);
                services.AddAction<WindowDockingLocationActionSettings, WindowDockingLocationActionSettingsControl>("classisland.settings.windowDockingLocation", "窗口停靠位置", PackIconKind.Monitor);
                services.AddAction<RunActionSettings, RunActionSettingsControl>("classisland.os.run", "运行", PackIconKind.OpenInApp);
                services.AddAction<NotificationActionSettings, NotificationActionSettingsControl>(
                    "classisland.showNotification", "显示提醒", PackIconKind.BellOutline);
                services.AddAction<SleepActionSettings, SleepActionSettingsControl>("classisland.action.sleep", "等待时长", PackIconKind.TimerSand);
                services.AddAction<WeatherNotificationActionSettings, WeatherNotificationActionSettingControl>(
                    "classisland.notification.weather", "显示天气提醒", PackIconKind.SunWirelessOutline);
                services.AddAction("classisland.app.quit", "退出 ClassIsland", PackIconKind.ExitToApp, (_, _) => Current.Stop());
                // 行动处理
                services.AddHostedService<RunActionHandler>();
                services.AddHostedService<AppSettingsActionHandler>();
                services.AddHostedService<SleepActionHandler>();
                // 认证提供方
                services.AddAuthorizeProvider<PasswordAuthorizeProvider>();
                // Plugins
                PluginService.InitializePlugins(context, services);
            }).Build();
        Logger = GetService<ILogger<App>>();
        Logger.LogInformation("ClassIsland {}", AppVersionLong);
        var lifetime = IAppHost.GetService<IHostApplicationLifetime>();
        lifetime.ApplicationStarted.Register(() => Logger.LogInformation("App started."));
        lifetime.ApplicationStopping.Register(() => Logger.LogInformation("App stopping."));
        lifetime.ApplicationStopped.Register(() => Logger.LogInformation("App stopped."));
        lifetime.ApplicationStopping.Register(Stop);
        if (ApplicationCommand.Verbose)
        {
            AppDomain.CurrentDomain.FirstChanceException += (o, args) => Logger.LogTrace(args.Exception, "发生内部异常");
            AppDomain.CurrentDomain.AssemblyLoad += (o, args) => Logger.LogTrace("加载程序集：{} ({})", args.LoadedAssembly.FullName, args.LoadedAssembly.Location);
        }
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
        Logger.LogInformation("初始化应用。");

        TransitionAssist.DisableTransitionsProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(Settings.IsTransientDisabled));
        IThemeService.IsTransientDisabled = Settings.IsTransientDisabled;
        IThemeService.IsWaitForTransientDisabled = Settings.IsWaitForTransientDisabled;
        if (Settings.IsSplashEnabled && !ApplicationCommand.Quiet)
        {
            var spanShowSplash = spanLaunching.StartChild("startup-show-splash");
            var splashDispatcher = await AsyncBox.RelatedAsyncDispatchers.GetOrAdd(Dispatcher, dispatcher => UIDispatcher.RunNewAsync("AsyncBox"));
            splashDispatcher.Invoke(() =>
            {
                GetService<SplashWindow>().Show();
            });
            spanShowSplash.Finish();
        }
        GetService<ISplashService>().CurrentProgress = 30;
        GetService<ISplashService>().SetDetailedStatus("正在启动挂起检查服务");

        var spanStartHangService = spanLaunching.StartChild("startup-start-hang-service");
        GetService<IHangService>();
        spanStartHangService.Finish();

        GetService<ISplashService>().SetDetailedStatus("正在创建任务栏图标");
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

        if (!ApplicationCommand.Quiet)  // 在静默启动时不进行更新相关操作
        {
            GetService<ISplashService>().SetDetailedStatus("正在进行更新服务启动操作");
            var spanCheckUpdate = spanLaunching.StartChild("startup-process-update");
            var r = await GetService<UpdateService>().AppStartup();
            spanCheckUpdate.Finish();
            if (r)
            {
                GetService<ISplashService>().EndSplash();
                return;
            }
        }
        GetService<ISplashService>().CurrentProgress = 45;

        GetService<ISplashService>().SetDetailedStatus("正在加载档案");
        await GetService<IProfileService>().LoadProfileAsync();
        GetService<IWeatherService>();
        GetService<IExactTimeService>();
        _ = GetService<WallpaperPickingService>().GetWallpaperAsync();
        _ = IAppHost.Host.StartAsync();
        IAppHost.GetService<IPluginMarketService>().LoadPluginSource();

        var spanLoadMainWindow = spanLaunching.StartChild("span-loading-mainWindow");
        Logger.LogInformation("正在初始化MainWindow。");
        GetService<ISplashService>().SetDetailedStatus("正在启动主界面所需的服务");
        GetService<ISplashService>().CurrentProgress = 55;
#if DEBUG
        MemoryProfiler.GetSnapshot("Pre MainWindow init");
#endif
        var mw = GetService<MainWindow>();
        MainWindow = mw;
        mw.StartupCompleted += (o, args) =>
        {
            GetService<ISplashService>().CurrentProgress = 98;
            GetService<ISplashService>().SetDetailedStatus("正在进行启动后操作");
            // 由于在应用启动时调用 WMI 会导致无法使用触摸，故在应用启动完成后再获取设备统计信息。
            // https://github.com/dotnet/wpf/issues/9752
            if (IsSentryEnabled)
            {
                DiagnosticService.GetDeviceInfo(out var name, out var vendor);
                SentrySdk.ConfigureScope(s =>
                {
                    s.SetTag("deviceDesktop.name", name);
                    s.SetTag("deviceDesktop.vendor", vendor);
                });
            }
            AppStarted?.Invoke(this, EventArgs.Empty);
            GetService<IIpcService>().IpcProvider.StartServer();
            GetService<IIpcService>().JsonRoutedProvider.StartServer();
            spanLoadMainWindow.Finish();
            transaction.Finish();
            SentrySdk.ConfigureScope(s => s.Transaction = null);
            GetService<IAutomationService>();
            GetService<IRulesetService>().NotifyStatusChanged();
            if (Settings.IsSplashEnabled)
            {
                App.GetService<ISplashService>().EndSplash();
            }
            _isStartedCompleted = true;
        };
#if DEBUG
        MemoryProfiler.GetSnapshot("Pre MainWindow show");
#endif
        GetService<ISplashService>().CurrentProgress = 80;
        GetService<ISplashService>().SetDetailedStatus("正在初始化主界面（步骤 2/2）");
        GetService<MainWindow>().Show();
        GetService<IWindowRuleService>();
        GetService<SignalTriggerHandlerService>();

        // 注册uri导航
        var uriNavigationService = GetService<IUriNavigationService>();
        uriNavigationService.HandleAppNavigation("test", args => CommonDialog.ShowInfo($"测试导航：{args.Uri}"));
        uriNavigationService.HandleAppNavigation("settings", args => GetService<SettingsWindowNew>().OpenUri(args.Uri));
        uriNavigationService.HandleAppNavigation("profile", args => GetService<MainWindow>().OpenProfileSettingsWindow());
        uriNavigationService.HandleAppNavigation("helps", args => uriNavigationService.Navigate(new Uri("https://docs.classisland.tech/app/")));
        uriNavigationService.HandleAppNavigation("profile/import-excel", args => GetService<ExcelImportWindow>().Show());

        GetService<IIpcService>().IpcProvider.CreateIpcJoint<IFooService>(new FooService());
        try
        {
            await App.GetService<FileFolderService>().ProcessAutoBackupAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "无法创建自动备份。");
        }

        if (ApplicationCommand.UpdateDeleteTarget != null)
        {
            GetService<SettingsService>().Settings.LastUpdateStatus = UpdateStatus.UpToDate;
            GetService<ITaskBarIconService>().ShowNotification("更新完成。", $"应用已更新到版本{AppVersion}。点击此处以查看更新日志。", clickedCallback:() => uriNavigationService.NavigateWrapped(new Uri("classisland://app/settings/update")));
        }
    }

    private TopmostEffectWindow BuildTopmostEffectWindow(IServiceProvider x)
    {
        
        var windowDispatcherAwaiter = AsyncBox.RelatedAsyncDispatchers.GetOrAdd(Dispatcher, dispatcher => UIDispatcher.RunNewAsync("AsyncBox"));
        while (!windowDispatcherAwaiter.IsCompleted)
        {
        }

        return windowDispatcherAwaiter.Result.Invoke(() =>
        {
            var window = new TopmostEffectWindow(x.GetRequiredService<ILogger<TopmostEffectWindow>>(), x.GetRequiredService<SettingsService>());
            return window;
        });
        
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
            .SetIconKind(CommonDialogIconKind.Hint)
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
        Dispatcher.Invoke(async () =>
        {
            AppStopping?.Invoke(this, EventArgs.Empty);
            IAppHost.Host?.Services.GetService<ILessonsService>()?.StopMainTimer();
            IAppHost.Host?.StopAsync(TimeSpan.FromSeconds(5));
            IAppHost.Host?.Services.GetService<SettingsService>()?.SaveSettings("停止当前应用程序。");
            IAppHost.Host?.Services.GetService<IAutomationService>()?.SaveConfig("停止当前应用程序。");
            IAppHost.Host?.Services.GetService<IProfileService>()?.SaveProfile();
            Current.Shutdown();
            if (AsyncBox.RelatedAsyncDispatchers.TryGetValue(Dispatcher, out var asyncDispatcherAwaiter))
            {
                var asyncDispatcher = await asyncDispatcherAwaiter;
                if (!asyncDispatcher.HasShutdownStarted)
                {
                    asyncDispatcher.InvokeShutdown();
                }
            }
            try
            {
                //ReleaseLock();
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "无法释放 Mutex。");
            }
        });
    }

    public override bool IsAssetsTrimmed() => IsAssetsTrimmedInternal;
    public override event EventHandler? AppStarted;
    public override event EventHandler? AppStopping;

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
