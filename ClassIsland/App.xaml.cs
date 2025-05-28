using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;
using ClassIsland.Controls.AttachedSettingsControls;
using ClassIsland.Controls.Components;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Commands;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Controls.CommonDialog;
using ClassIsland.Core.Extensions.Registry;
using ClassIsland.Shared;
using ClassIsland.Shared.Abstraction.Services;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.Services.AppUpdating;
using ClassIsland.Services.Logging;
using ClassIsland.Services.Management;
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
using ClassIsland.Core;
using ClassIsland.Core.Models.Ruleset;
using Sentry;
using ClassIsland.Core.Controls.Ruleset;
using ClassIsland.Models.Rules;
using ClassIsland.Models.Actions;
using ClassIsland.Controls.RuleSettingsControls;
using ClassIsland.Shared.IPC.Abstractions.Services;
using dotnetCampus.Ipc.CompilerServices.GeneratedProxies;
using ClassIsland.Controls.ActionSettingsControls;
using ClassIsland.Controls.AuthorizeProvider;
using ClassIsland.Core.Enums;
using ClassIsland.Services.ActionHandlers;
#if IsMsix
using Windows.ApplicationModel;
using Windows.Storage;
#endif
using ClassIsland.Services.Automation.Triggers;
using ClassIsland.Controls.TriggerSettingsControls;
using ClassIsland.Core.Abstractions.Services.Metadata;
using ClassIsland.Core.Abstractions.Views;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models.Logging;
using ClassIsland.Models.Automation.Triggers;
using ClassIsland.Services.Metadata;
using ClassIsland.Shared.Helpers;
using Microsoft.Extensions.Logging.Console;
using Walterlv.Threading;
using Walterlv.Windows;
using ClassIsland.Controls.NotificationProviders;
using System.Text;
using ClassIsland.Controls.SpeechProviderSettingsControls;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Shared.Protobuf.AuditEvent;
using ClassIsland.Shared.Protobuf.Enum;
using Google.Protobuf.WellKnownTypes;

namespace ClassIsland;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : AppBase, IAppHost
{
    internal Dispatcher? ThreadedUiDispatcher { get; set; }

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

    internal static bool IsCrashed { get; set; } = false;

    internal static bool _isCriticalSafeModeEnabled = false;

    internal static bool AutoDisableCorruptPlugins
    {
        get
        {
            try
            {
                return ((App)Current).Settings.AutoDisableCorruptPlugins;
            }
            catch
            {
                return true;
            }
        }
    }

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

    public override string OperatingSystem => "windows";
    public override string Platform =>
#if PLATFORM_x64
    "x64"
#elif PLATFORM_x86
    "x86"
#elif PLATFORM_ARM64
    "arm64"
#elif PLATFORM_ARM
    "arm"
#elif PLATFORM_Any
    "any"
#else
    #if PublishBuilding
    #error "在发布构建中不应出现未知架构"
    #endif
    "unknown"
#endif
        ;
    public App()
    {
        //AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.EnablePointerSupport", true);
        //TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
    }

    private static void CurrentDomainOnProcessExit(object? sender, EventArgs e)
    {
        if (IsCrashed)
        {
            return;
        }

        try
        {
            var startupCountFilePath = Path.Combine(AppRootFolderPath, ".startup-count");
            if (File.Exists(startupCountFilePath))
            {
                File.Delete(startupCountFilePath);
            }
        }
        catch (Exception exception)
        {
            // ignored
        }
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
        // COMException: UCEERR_RENDERTHREADFAILURE (0x88980406)
        if (e.Exception is COMException comException && (uint)comException.HResult == 0x88980406)
        {
            ProcessWpfCriticalException(e.Exception);
            return;
        }
        try
        {
            ProcessUnhandledException(e.Exception);
        }
        catch
        {
            ProcessWpfCriticalException(e.Exception);
        }
        e.Handled = true;
    }

    private void ProcessWpfCriticalException(Exception ex)
    {
        Exit += (_, _) =>
        {
            DiagnosticService.ProcessCriticalException(ex);
        };
        Stop();
    }

    internal void ProcessUnhandledException(Exception e, bool critical=false)
    {
#if DEBUG
        if (e.GetType() == typeof(ResourceReferenceKeyNotFoundException))
        {
            return;
        }
#endif
        Logger?.LogCritical(e, "发生严重错误");
        IsCrashed = true;
        var safe = _isCriticalSafeModeEnabled && (!(IAppHost.TryGetService<IWindowRuleService>()?.IsForegroundWindowClassIsland() ?? false));

        //Settings.DiagnosticCrashCount++;
        //Settings.DiagnosticLastCrashTime = DateTime.Now;

        if (!critical)  // 全局未捕获的异常应该由 SentrySdk 自行捕获。
        {
            SentrySdk.CaptureException(e, scope =>
            {
                scope.Level = SentryLevel.Fatal;
            });
        }

        var plugins = DiagnosticService.GetPluginsByStacktrace(e);
        var disabled = DiagnosticService.DisableCorruptPlugins(plugins);
        var managementService = IAppHost.TryGetService<IManagementService>();
        if (managementService is IManagementService { IsManagementEnabled: true, Connection: ManagementServerConnection connection })
        {
            connection.LogAuditEvent(AuditEvents.AppCrashed, new AppCrashed()
            {
                Stacktrace = e.ToString()
            });
        }
        if (!safe)
        {
            var traceId = SentrySdk.GetTraceHeader()?.TraceId;
            var crashInfo = e.ToString();
            if (plugins.Count > 0)
            {
                var pluginsWarning = "此问题可能由以下插件引起，请在向 ClassIsland 开发者反馈问题前先向以下插件的开发者反馈此问题：\n"
                                     + string.Join("\n", plugins.Select(x => $"- {x.Manifest.Name} [{x.Manifest.Id}]"))
                                     + (disabled
                                         ? "\n以上异常插件已自动禁用，重启应用后生效。您可以在排除问题后前往【应用设置】->【插件】中重新启用这些插件，或在【应用设置】->【基本】中调整是否自动禁用异常插件。"
                                         : "")
                    + "\n================================\n";
                crashInfo = pluginsWarning + crashInfo;
            }
            if (traceId != null)
            {
                var traceInfo = $"""
                                 在向开发者提交问题时请保留以下信息：
                                 TraceID: {traceId}
                                 ================================
                                 
                                 """;
                crashInfo = traceInfo + crashInfo;
            }
            CrashWindow = new CrashWindow()
            {
                CrashInfo = crashInfo,
                AllowIgnore = _isStartedCompleted && !critical,
                IsCritical = critical
            };
            CrashWindow.ShowDialog();
            return;
        }

        switch (Settings.CriticalSafeModeMethod)
        {
            case 2 when critical:
            case 3 when critical:
            case 0:
                Logger?.LogInformation("因教学安全模式设定，应用将自动退出");
                Stop();
                break;
            case 1:
                Logger?.LogInformation("因教学安全模式设定，应用将自动静默重启");
                Restart(["-q", "-m"]);
                break;
            case 2:
                Logger?.LogInformation("因教学安全模式设定，应用将忽略异常并显示一条通知");
                IAppHost.Host?.Services.GetService<ITaskBarIconService>()?.ShowNotification("崩溃报告", $"ClassIsland 发生了一个无法处理的错误：{e.Message}");
                break;
            case 3:
                Logger?.LogInformation("因教学安全模式设定，应用将直接忽略异常");
                break;
            default:
                Logger?.LogWarning("无效的教学安全模式设置：{}", Settings.CriticalSafeModeMethod);
                break;
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
        AppBase.CurrentLifetime = ApplicationLifetime.Initializing;
        MyWindow.ShowOssWatermark = ApplicationCommand.ShowOssWatermark;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        //DependencyPropertyHelper.ForceOverwriteDependencyPropertyDefaultValue(FrameworkElement.FocusVisualStyleProperty,
        //    Resources[SystemParameters.FocusVisualStyleKey]);
        Environment.CurrentDirectory = System.Windows.Forms.Application.StartupPath;

        //ConsoleService.InitializeConsole();
        System.Windows.Forms.Application.EnableVisualStyles();
        DiagnosticService.BeginStartup();
        ConsoleService.InitializeConsole();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
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
            return;
        }

        // 检测桌面文件夹
        if (Environment.CurrentDirectory == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) && !Settings.IsWelcomeWindowShowed)
        {
            var r = CommonDialog.ShowHint("ClassIsland正在桌面上运行，应用设置、课表等数据将会直接存放到桌面上。在使用本应用前，请将本应用移动到一个单独的文件夹中。");
            if (r == 0)
            {
                Environment.Exit(0);
                return;
            }
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
            return;
        }

        // 检测 DWM
        DwmIsCompositionEnabled(out var isDwmEnabled);
        if (!isDwmEnabled)
        {
            CommonDialog.ShowError("运行ClassIsland需要开启Aero效果。请在【控制面板】->【个性化】中启用Aero主题，然后再尝试运行ClassIsland。");
            Environment.Exit(0);
            return;
        }

        var startupCountFilePath = Path.Combine(AppRootFolderPath, ".startup-count");
        var startupCount = File.Exists(startupCountFilePath)
            ? (int.TryParse(await File.ReadAllTextAsync(startupCountFilePath), out var count) ? count + 1 : 1)
            : 1;
        if (startupCount >= 5 && ApplicationCommand is { Recovery: false, Quiet: false })
        {
            var enterRecovery = new CommonDialogBuilder()
                .SetIconKind(CommonDialogIconKind.Hint)
                .SetContent("ClassIsland 多次启动失败，您需要进入恢复模式以尝试修复 ClassIsland 吗？")
                .AddCancelAction()
                .AddAction("进入恢复模式", PackIconKind.WrenchCheckOutline, true)
                .ShowDialog();
            if (enterRecovery == 1)
            {
                ApplicationCommand.Recovery = true;
            }
        }
        if (ApplicationCommand.Recovery)
        {
            if (File.Exists(startupCountFilePath))
            {
                File.Delete(startupCountFilePath);
            }
            
            var recoveryWindow = new RecoveryWindow();
            recoveryWindow.Show();
            transaction.Finish();
            return;
        }

        
        await File.WriteAllTextAsync(startupCountFilePath, startupCount.ToString());
        AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;

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
                services.AddSingleton<ISpeechService>(GetSpeechService);
                services.AddSingleton<IExactTimeService, ExactTimeService>();
                //services.AddSingleton(typeof(ApplicationCommand), ApplicationCommand);
                services.AddSingleton<IProfileAnalyzeService, ProfileAnalyzeService>();
                services.AddSingleton<IIpcService, IpcService>();
                services.AddSingleton<IAuthorizeService, AuthorizeService>();
                services.AddSingleton<UriTriggerHandlerService>();
                services.AddSingleton<SignalTriggerHandlerService>();
                services.AddSingleton<IAnnouncementService, AnnouncementService>();
                services.AddSingleton<ILocationService, LocationService>();
                services.AddSingleton<IXamlThemeService, XamlThemeService>();
                // Views
                services.AddSingleton<MainWindow>();
                services.AddTransient<SplashWindowBase, SplashWindow>();
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
                services.AddTransient<ConfigErrorsWindow>();
                services.AddTransient<TimeAdjustmentWindow>();
                services.AddTransient<ExcelExportWindow>();
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
                services.AddSettingsPage<ThemesSettingsPage>();
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
                services.AddComponent<RollingComponent, RollingComponentSettingsControl>();
                services.AddComponent<GroupComponent>();
                // 提醒提供方
                services.AddNotificationProvider<ClassNotificationProvider, ClassNotificationProviderSettingsControl>();
                services.AddNotificationProvider<AfterSchoolNotificationProvider, AfterSchoolNotificationProviderSettingsControl>();
                services.AddNotificationProvider<WeatherNotificationProvider, WeatherNotificationProviderSettingsControl>();
                services.AddNotificationProvider<ManagementNotificationProvider>();
                services.AddNotificationProvider<ActionNotificationProvider>();
                // Transients
                services.AddTransient<ExcelImportWindow>();
                services.AddTransient<WallpaperPreviewWindow>();
                // Logging
                services.AddLogging(builder =>
                {
                    LogMaskingHelper.Rules.Add(new LogMaskRule(new(@"(latitude=)(\d*\.?\d*)"), 2));
                    LogMaskingHelper.Rules.Add(new LogMaskRule(new(@"(longitude=)(\d*\.?\d*)"), 2));

                    builder.AddConsoleFormatter<ClassIslandConsoleFormatter, ConsoleFormatterOptions>();
                    builder.AddConsole(console =>
                    {
                        console.FormatterName = "classisland";
                    });
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
                services.AddTrigger<PreTimePointTrigger, PreTimePointTriggerSettingsControl>();
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
                services.AddAction<WindowLayerActionSettings, WindowLayerActionSettingsControl>("classisland.settings.windowLayer", "窗口层级", PackIconKind.LayersOutline);
                services.AddAction<WindowDockingOffsetXActionSettings, WindowDockingOffsetXActionSettingsControl>("classisland.settings.windowDockingOffsetX", "窗口向右偏移", PackIconKind.ArrowCollapseRight);
                services.AddAction<WindowDockingOffsetYActionSettings, WindowDockingOffsetYActionSettingsControl>("classisland.settings.windowDockingOffsetY", "窗口向下偏移", PackIconKind.ArrowCollapseDown);
                services.AddAction<RunActionSettings, RunActionSettingsControl>("classisland.os.run", "运行", PackIconKind.OpenInApp);
                services.AddAction<NotificationActionSettings, NotificationActionSettingsControl>(
                    "classisland.showNotification", "显示提醒", PackIconKind.BellOutline);
                services.AddAction<SleepActionSettings, SleepActionSettingsControl>("classisland.action.sleep", "等待时长", PackIconKind.TimerSand);
                services.AddAction<WeatherNotificationActionSettings, WeatherNotificationActionSettingControl>(
                    "classisland.notification.weather", "显示天气提醒", PackIconKind.SunWirelessOutline);
                services.AddAction("classisland.app.quit", "退出 ClassIsland", PackIconKind.ExitToApp, (_, _) => Current.Stop());
                services.AddAction<AppRestartActionSettings,AppRestartActionSettingsControl>("classisland.app.restart", "重启 ClassIsland", PackIconKind.Restart);
                // 行动处理
                services.AddHostedService<AppRestartActionHandler>();
                services.AddHostedService<RunActionHandler>();
                services.AddHostedService<AppSettingsActionHandler>();
                services.AddHostedService<SleepActionHandler>();
                // 认证提供方
                services.AddAuthorizeProvider<PasswordAuthorizeProvider>();
                // 语音提供方
                services.AddSpeechProvider<SystemSpeechService>();
                services.AddSpeechProvider<EdgeTtsService, EdgeTtsSpeechServiceSettingsControl>();
                services.AddSpeechProvider<GptSoVitsService, GptSovitsSpeechServiceSettingsControl>();
                // 天气图标模板
                var materialDesignWeatherIconTemplateDictionary = new ResourceDictionary()
                {
                    Source = new Uri("pack://application:,,,/ClassIsland;component/Controls/WeatherIcons/MaterialDesignWeatherIconTemplate.xaml")
                };
                services.AddWeatherIconTemplate("classisland.weatherIcons.materialDesign", "Material Design（默认）",
                    (DataTemplate)materialDesignWeatherIconTemplateDictionary["MaterialDesignWeatherIconTemplate"]!);
                var simpleTextWeatherIconTemplateDictionary = new ResourceDictionary()
                {
                    Source = new Uri("pack://application:,,,/ClassIsland;component/Controls/WeatherIcons/SimpleTextWeatherIconTemplate.xaml")
                };
                services.AddWeatherIconTemplate("classisland.weatherIcons.simpleText", "纯文本",
                    (DataTemplate)simpleTextWeatherIconTemplateDictionary["SimpleTextWeatherIconTemplate"]!);

                // Plugins
                if (!ApplicationCommand.Safe)
                {
                    PluginService.InitializePlugins(context, services);
                }
            }).Build();
        AppBase.CurrentLifetime = ApplicationLifetime.Starting;
        Logger = GetService<ILogger<App>>();
        Logger.LogInformation("ClassIsland {}", AppVersionLong);
        var lifetime = IAppHost.GetService<IHostApplicationLifetime>();
        lifetime.ApplicationStarted.Register(() => Logger.LogInformation("App started."));
        lifetime.ApplicationStopping.Register(() =>
        {
            Logger.LogInformation("App stopping.");
            Stop();
        });
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
        var threadedUiDispatcherAwaiter =
            AsyncBox.RelatedAsyncDispatchers.GetOrAdd(Dispatcher, dispatcher => UIDispatcher.RunNewAsync("AsyncBox"));
        await Task.Run(() =>
        {
            while (!threadedUiDispatcherAwaiter.IsCompleted)
            {
            }
        });
        ThreadedUiDispatcher = threadedUiDispatcherAwaiter.Result;
        Logger.LogInformation("初始化应用。");

        TransitionAssist.DisableTransitionsProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(Settings.IsTransientDisabled));
        IThemeService.IsTransientDisabled = Settings.IsTransientDisabled;
        IThemeService.IsWaitForTransientDisabled = Settings.IsWaitForTransientDisabled;
        if (Settings.IsSplashEnabled && !ApplicationCommand.Quiet)
        {
            var spanShowSplash = spanLaunching.StartChild("startup-show-splash");

            ThreadedUiDispatcher.Invoke(() =>
            {
                GetService<SplashWindowBase>().Show();
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
        await GetService<IComponentsService>().LoadManagementConfig();
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
            File.Delete(startupCountFilePath);
            if (ConfigureFileHelper.Errors.FirstOrDefault(x => x.Critical) != null)
            {
                GetService<ITaskBarIconService>().ShowNotification("配置文件损坏", "ClassIsland 部分配置文件已损坏且无法加载，这些配置文件已恢复至默认值。点击此消息以查看详细信息和从过往备份中恢复配置文件。", clickedCallback:() => GetService<IUriNavigationService>().NavigateWrapped(new Uri("classisland://app/config-errors")));
            }
            if (Settings.CorruptPluginsDisabledLastSession)
            {
                Settings.CorruptPluginsDisabledLastSession = false;
                GetService<ITaskBarIconService>().ShowNotification("已自动禁用异常插件", "ClassIsland 已自动禁用导致上次崩溃的插件。您可以在排除问题后前往【应用设置】->【插件】中重新启用这些插件，或在【应用设置】->【基本】中调整是否自动禁用异常插件。", clickedCallback: () => GetService<IUriNavigationService>().NavigateWrapped(new Uri("classisland://app/settings/classisland.plugins")));
            }
            if (Settings.IsSplashEnabled)
            {
                App.GetService<ISplashService>().EndSplash();
            }
            if (IAppHost.TryGetService<IManagementService>() is IManagementService { IsManagementEnabled: true, Connection: ManagementServerConnection connection })
            {
                connection.LogAuditEvent(AuditEvents.AppStarted, new Empty());
            }
            _isStartedCompleted = true;
            AppBase.CurrentLifetime = ApplicationLifetime.Running;
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
        uriNavigationService.HandleAppNavigation("config-errors", args => GetService<ConfigErrorsWindow>().ShowDialog());

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

    private ISpeechService GetSpeechService(IServiceProvider provider)
    {
        try
        {
            var service = IAppHost.Host?.Services.GetKeyedService<ISpeechService>(Settings.SelectedSpeechProvider);
            if (service == null)
            {
                throw new InvalidOperationException($"语音提供方 {Settings.SelectedSpeechProvider} 未注册");
            }
            return service;
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "无法初始化语音提供方 {}", Settings.SelectedSpeechProvider);
        }
        return new BlankSpeechService();
        
    }

    private TopmostEffectWindow BuildTopmostEffectWindow(IServiceProvider x)
    {
        var dispatcher = (Settings.NotificationUseStandaloneEffectUiThread ? ThreadedUiDispatcher : Dispatcher) 
                         ?? Dispatcher;
        return dispatcher.Invoke(() =>
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
        InstanceExistedWindow popup = new();
        bool needRestart = popup.ShowDialog() ?? false;
        if (!needRestart)
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
        if (CurrentLifetime == ApplicationLifetime.Stopping)
        {
            return;
        }
        Dispatcher.Invoke(async () =>
        {
            CurrentLifetime = ApplicationLifetime.Stopping;
            Logger?.LogInformation("正在停止应用");
            if (IAppHost.TryGetService<IManagementService>() is { IsManagementEnabled: true, Connection: ManagementServerConnection connection })
            {
                connection.LogAuditEvent(AuditEvents.AppExited, new Empty());
            }
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
        if (quiet)
        {
            Restart(["-m", "-q"]);
        }
        else
        {
            Restart(["-m"]);
        }
        
    }

    public override void Restart(string[] parameters)
    {
        Stop();
        var path = Environment.ProcessPath;
        if (path == null)
            return;
        var replaced = path.Replace(".dll", ".exe");
        var startInfo = new ProcessStartInfo(replaced);
        foreach (var i in parameters)
        {
            startInfo.ArgumentList.Add(i);
        }
        Process.Start(startInfo);
    }
}
