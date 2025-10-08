
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Commands;
using ClassIsland.Core.Controls;
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
// using ClassIsland.Views.SettingPages;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
using ClassIsland.Shared.IPC.Abstractions.Services;
using dotnetCampus.Ipc.CompilerServices.GeneratedProxies;
using ClassIsland.Core.Enums;
using ClassIsland.Services.ActionHandlers;
using ClassIsland.Services.Automation.Triggers;
using ClassIsland.Core.Abstractions.Services.Metadata;
using ClassIsland.Core.Abstractions.Views;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models.Logging;
using ClassIsland.Models.Automation.Triggers;
using ClassIsland.Services.Metadata;
using ClassIsland.Shared.Helpers;
using Microsoft.Extensions.Logging.Console;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Labs.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Controls.ActionSettingsControls;
using ClassIsland.Controls.AttachedSettingsControls;
using ClassIsland.Controls.AuthorizeProvider;
using ClassIsland.Controls.Components;
using ClassIsland.Controls.NotificationProviders;
using ClassIsland.Controls.RuleSettingsControls;
using ClassIsland.Controls.SpeechProviderSettingsControls;
using ClassIsland.Controls.TriggerSettingsControls;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.XamlTheme;
using ClassIsland.Enums;
using ClassIsland.Helpers;
using ClassIsland.Models.Actions;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Enums;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Services.Automation.Actions;
using ClassIsland.Shared.Protobuf.AuditEvent;
using ClassIsland.Shared.Protobuf.Enum;
using ClassIsland.ViewModels;
using ClassIsland.ViewModels.SettingsPages;
using ClassIsland.Views.SettingPages;
using FluentAvalonia.UI.Controls;
using Google.Protobuf.WellKnownTypes;
using HotAvalonia;
using ReactiveUI;
using Empty = Google.Protobuf.WellKnownTypes.Empty;
using UpdateSettingsPage = ClassIsland.Views.SettingPages.UpdateSettingsPage;

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

    public override string OperatingSystem { get; internal set; } = "unknown";

    public override string PackagingType { get; internal set; } = "folderClassic";

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

    public EventHandler? Initialized;

    public App()
    {
        //AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.EnablePointerSupport", true);
        //TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
    }

    private void ActivatePackageType()
    {
        if (IsMsix)
        {
            PackagingType = "msix";
            return;
        }
        var packageTypeDir = Path.Combine(Environment.GetEnvironmentVariable("ClassIsland_PackageRoot") ?? "./",
            "PackageType");
        var fallbackPackageTypeDir = Path.Combine("../", "PackageType");
        var packagingRoot = Path.GetFullPath(Environment.GetEnvironmentVariable("ClassIsland_PackageRoot") ?? "../");
        if (File.Exists(packageTypeDir))
        {
            try
            {
                PackagingType = File.ReadAllText(packageTypeDir);
                CommonDirectories.AppPackageRoot = packagingRoot;
                return;
            }
            catch (Exception e)
            {
                // ignored
            }
        }

        if (File.Exists(fallbackPackageTypeDir))
        {
            try
            {
                PackagingType = File.ReadAllText(fallbackPackageTypeDir);
                CommonDirectories.AppPackageRoot = packagingRoot;
                return;
            }
            catch (Exception e)
            {
                // ignored
            }
        }
    }

    private void ActivateAppDirectories()
    {
        PackagingType = PackagingType.Replace("\n", "").Replace("\r", "");

        ExecutingEntrance = Environment.ProcessPath?.Replace(".dll", PlatformExecutableExtension) ?? "";
        CommonDirectories.AppRootFolderPath = PackagingType switch
        {
            "folder" => Path.Combine(CommonDirectories.AppPackageRoot, "data"),
            "installer" or "deb" or "appImage" or "pkg" or "msix" => Path.GetFullPath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClassIsland", "Data")),
            _ => System.OperatingSystem.IsMacOS() ? Path.GetFullPath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClassIsland", "Data")) :
                CommonDirectories.AppRootFolderPath
        };

        if (!Directory.Exists(CommonDirectories.AppRootFolderPath))
        {
            Directory.CreateDirectory(CommonDirectories.AppRootFolderPath);
        }

        if (File.Exists(Path.Combine(CommonDirectories.AppPackageRoot, "ClassIsland" + PlatformExecutableExtension)))
        {
            ExecutingEntrance =
                Path.Combine(CommonDirectories.AppPackageRoot, "ClassIsland" + PlatformExecutableExtension);
        }
        else
        {
            ExecutingEntrance = Environment.ProcessPath?.Replace(".dll", PlatformExecutableExtension) ?? "";
        }
    }

    public override void Initialize()
    {
        Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location) ?? ".";
        ActivatePackageType();
        ActivateAppDirectories();
        if (!Design.IsDesignMode && !System.OperatingSystem.IsMacOS())
        {
            this.EnableHotReload();
        }
        AvaloniaXamlLoader.Load(this);
        if (DesktopLifetime != null)
        {
            DesktopLifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }
        IconExpressionHelper.RegisterHandler("fluent", args => new FluentIconSource(args[0]));
        IconExpressionHelper.RegisterHandler("lucide", args => new LucideIconSource(args[0]));
        IconExpressionHelper.RegisterHandler("bitmap", args => new BitmapIconSource
        {
            UriSource = new Uri(args[0]),
            ShowAsMonochrome = args.Length >= 2  && bool.TryParse(args[2], out var r1) && r1
        });
        base.Initialize();
    }

    private static void CurrentDomainOnProcessExit(object? sender, EventArgs e)
    {
        if (IsCrashed)
        {
            return;
        }

        try
        {
            var startupCountFilePath = Path.Combine(CommonDirectories.AppRootFolderPath, ".startup-count");
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

    private void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        Dispatcher.UIThread.Invoke(() =>
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
        Avalonia.Threading.Dispatcher.UIThread.ShutdownFinished += (_, _) =>
        {
            DiagnosticService.ProcessCriticalException(ex);
        };
        Stop();
    }

    internal async void ProcessUnhandledException(Exception e, bool critical=false)
    {
#if DEBUG
        // if (e.GetType() == typeof(ResourceReferenceKeyNotFoundException))
        // {
        //     return;
        // }
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
            await CrashWindow.ShowDialog(GetRootWindow());
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
                try
                {
                    await PlatformServices.DesktopToastService.ShowToastAsync("崩溃报告", $"ClassIsland 发生了一个无法处理的错误：{e.Message}");
                }
                catch (Exception exception)
                {
                    Logger?.LogError(exception, "显示通知失败");
                }
                break;
            case 3:
                Logger?.LogInformation("因教学安全模式设定，应用将直接忽略异常");
                break;
            default:
                Logger?.LogWarning("无效的教学安全模式设置：{}", Settings.CriticalSafeModeMethod);
                break;
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        DesktopLifetime!.Startup += DesktopLifetimeOnStartup;
        base.OnFrameworkInitializationCompleted();
    }

    private async void DesktopLifetimeOnStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {

        PhonyRootWindow = new Window()
        {
            Width = 1,
            Height = 1,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowActivated = false,
            SystemDecorations = SystemDecorations.None,
            ShowInTaskbar = false,
            Background = Brushes.Transparent,
            TransparencyLevelHint = [ WindowTransparencyLevel.Transparent ],
            Title = "PhonyRootWindow"
        };
        PhonyRootWindow.Closing += (sender, args) =>
        {
            if (args.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown)
            {
                return;
            }
            args.Cancel = true;
        };
        PhonyRootWindow.Show();
        PlatformServices.WindowPlatformService.SetWindowFeature(PhonyRootWindow, WindowFeatures.ToolWindow | WindowFeatures.SkipManagement | WindowFeatures.Transparent, true);
        Initialized?.Invoke(this, EventArgs.Empty);
        var transaction = SentrySdk.StartTransaction(
            "startup",
            "startup"
        );
        SentrySdk.ConfigureScope(s => s.Transaction = transaction);
        var spanPreInit = transaction.StartChild("startup-init");
        AppBase.CurrentLifetime = ClassIsland.Core.Enums.ApplicationLifetime.Initializing;
        Dispatcher.UIThread.UnhandledException += App_OnDispatcherUnhandledException;
        MyWindow.ShowOssWatermark = ApplicationCommand.ShowOssWatermark;
        //DependencyPropertyHelper.ForceOverwriteDependencyPropertyDefaultValue(FrameworkElement.FocusVisualStyleProperty,
        //    Resources[SystemParameters.FocusVisualStyleKey]);

        //ConsoleService.InitializeConsole();
        DiagnosticService.BeginStartup();
        ConsoleService.InitializeConsole();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        
        
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");
        Thread.CurrentThread.CurrentCulture = new CultureInfo("zh-CN");

        // 检测Mutex
        if (!IsMutexCreateNew && !Design.IsDesignMode)
        {
            if (!ApplicationCommand.WaitMutex)
            {
                spanPreInit.Finish();
                transaction.Finish();
                await ProcessInstanceExisted();
                Environment.Exit(0);
                return;
            }
        }

        // 检测临时目录
        if (Environment.CurrentDirectory.Contains(Path.GetTempPath()))
        {
            await CommonTaskDialogs.ShowDialog("检测到应用正在临时目录下运行", "ClassIsland正在临时目录下运行，应用设置、课表等数据很可能无法保存，或在应用退出后被自动删除。在使用本应用前，请务必将本应用解压到一个适合的位置。");
            Environment.Exit(0);
            return;
        }

        // 检测桌面文件夹
        if (Environment.CurrentDirectory == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) && !Settings.IsWelcomeWindowShowed)
        {
            var r = await CommonTaskDialogs.ShowDialog("检测到正在桌面上运行", "ClassIsland正在桌面上运行，应用设置、课表等数据将会直接存放到桌面上。在使用本应用前，请将本应用移动到一个单独的文件夹中。");
            if (r == (object)true)
            {
                Environment.Exit(0);
                return;
            }
        }

        // 检测目录是否可以访问
        try
        {
            var testWritePath = Path.Combine(CommonDirectories.AppRootFolderPath, "./.test-write");
            await File.WriteAllTextAsync(testWritePath, "");
            File.Delete(testWritePath);
        }
        catch (Exception ex)
        {
            await CommonTaskDialogs.ShowDialog("目录权限错误", $"ClassIsland无法写入当前目录：{ex.Message}\n\n请将本软件解压到一个合适的位置后再运行。");
            Environment.Exit(0);
            return;
        }

        var startupCountFilePath = Path.Combine(CommonDirectories.AppRootFolderPath, ".startup-count");
        var startupCount = File.Exists(startupCountFilePath)
            ? (int.TryParse(await File.ReadAllTextAsync(startupCountFilePath), out var count) ? count + 1 : 1)
            : 1;
        if (startupCount >= 5 && ApplicationCommand is { Recovery: false, Quiet: false })
        {
            var dialog = new TaskDialog()
            {
                Title = "进入恢复模式",
                Content = "ClassIsland 多次启动失败，您需要进入恢复模式以尝试修复 ClassIsland 吗？",
                XamlRoot = GetRootWindow(),
                Buttons =
                [
                    new TaskDialogButton("取消", false),
                    new TaskDialogButton("进入恢复模式", true)
                    {
                        IsDefault = true
                    }
                ],
            };
            var r = await dialog.ShowAsync();
            if (Equals(r, true))
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
                // services.AddSingleton<WallpaperPickingService>();
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
                services.AddSingleton<ILocationService>(PlatformServices.LocationService);
                services.AddSingleton<IXamlThemeService, XamlThemeService>();
                services.AddSingleton<IAudioService, AudioService>();
                // ViewModels
                services.AddTransient<ProfileSettingsViewModel>();
                services.AddTransient<DevPortalViewModel>();
                services.AddTransient<AppLogsViewModel>();
                services.AddTransient<WelcomeViewModel>();
                services.AddTransient<ClassChangingViewModel>();
                services.AddTransient<DataTransferViewModel>();
                // ViewModels/SettingsPages
                services.AddTransient<GeneralSettingsViewModel>();
                services.AddTransient<AboutSettingsViewModel>();
                services.AddTransient<AppearanceSettingsViewModel>();
                services.AddTransient<ComponentsSettingsViewModel>();
                services.AddTransient<NotificationSettingsViewModel>();
                services.AddTransient<WindowSettingsViewModel>();
                services.AddTransient<WeatherSettingsViewModel>();
                services.AddTransient<AutomationSettingsViewModel>();
                services.AddTransient<PluginsSettingsPageViewModel>();
                services.AddTransient<StorageSettingsViewModel>();
                services.AddTransient<ErrorSettingsViewModel>();
                services.AddTransient<ThemesSettingsViewModel>();
                services.AddTransient<UpdateSettingsPageViewModel>();
                // Views
                services.AddSingleton<MainWindow>();
                // services.AddTransient<SplashWindowBase, SplashWindow>();
                // services.AddTransient<FeatureDebugWindow>();
                services.AddSingleton<TopmostEffectWindow>();
                services.AddSingleton<AppLogsWindow>();
                services.AddSingleton<SettingsWindowNew>();
                services.AddSingleton<ProfileSettingsWindow>();
                services.AddTransient<ClassPlanDetailsWindow>();
                services.AddTransient<WindowRuleDebugWindow>();
                // services.AddTransient<ConfigErrorsWindow>();
                services.AddTransient<TimeAdjustmentWindow>();
                // services.AddTransient<ExcelExportWindow>();
                services.AddTransient<DevPortalWindow>();
                services.AddTransient<WelcomeWindow>();
                services.AddTransient<DataTransferWindow>();
                services.AddTransient<DebugPageViewModel>();
                // 设置页面
                services.AddSettingsPage<GeneralSettingsPage>();
                services.AddSettingsPage<ComponentsSettingsPage>();
                services.AddSettingsPage<AppearanceSettingsPage>();
                services.AddSettingsPage<NotificationSettingsPage>();
                services.AddSettingsPage<WindowSettingsPage>();
                services.AddSettingsPage<WeatherSettingsPage>();
                services.AddSettingsPage<AutomationSettingsPage>();
                services.AddSettingsPage<UpdateSettingsPage>();
                services.AddSettingsPage<StorageSettingsPage>();
                services.AddSettingsPage<PrivacySettingsPage>();
                services.AddSettingsPage<PluginsSettingsPage>();
                services.AddSettingsPage<ThemesSettingsPage>();
                services.AddSettingsPage<TestSettingsPage>();
                services.AddSettingsPage<DebugPage>();
                // services.AddSettingsPage<DebugBrushesSettingsPage>();
                services.AddSettingsPage<AboutSettingsPage>();
                services.AddSettingsPage<ManagementSettingsPage>();
                services.AddSettingsPage<ManagementCredentialsSettingsPage>();
                services.AddSettingsPage<ManagementPolicySettingsPage>();
                services.AddSettingsPage<ErrorSettingsPage>();
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
                services.AddComponent<StackComponent>();
                // 提醒提供方
                services.AddNotificationProvider<ClassNotificationProvider, ClassNotificationProviderSettingsControl>();
                services.AddNotificationProvider<AfterSchoolNotificationProvider, AfterSchoolNotificationProviderSettingsControl>();
                services.AddNotificationProvider<WeatherNotificationProvider, WeatherNotificationProviderSettingsControl>();
                // services.AddNotificationProvider<ManagementNotificationProvider>();
                services.AddNotificationProvider<ActionNotificationProvider>();
                // // Transients
                // services.AddTransient<ExcelImportWindow>();
                // services.AddTransient<WallpaperPreviewWindow>();
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
                // // 触发器
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
                services.AddRule<StringMatchingSettings, RulesetStringMatchingSettingsControl>("classisland.windows.className", "前台窗口类名", "\uF4A2");
                services.AddRule<StringMatchingSettings, RulesetStringMatchingSettingsControl>("classisland.windows.text", "前台窗口标题", "\uF26B");
                services.AddRule<WindowStatusRuleSettings, WindowStatusRuleSettingsControl>("classisland.windows.status", "前台窗口状态是", "\uEC83");
                services.AddRule<StringMatchingSettings, RulesetStringMatchingSettingsControl>("classisland.windows.processName", "前台窗口进程", "\uF488");
                services.AddRule<CurrentSubjectRuleSettings, CurrentSubjectRuleSettingsControl>("classisland.lessons.currentSubject", "科目是", "\uE215");
                services.AddRule<CurrentSubjectRuleSettings, CurrentSubjectRuleSettingsControl>("classisland.lessons.nextSubject", "下节课科目是", "\uE217");
                services.AddRule<CurrentSubjectRuleSettings, CurrentSubjectRuleSettingsControl>("classisland.lessons.previousSubject", "上节课科目是", "\uE226");
                services.AddRule<TimeStateRuleSettings, TimeStateRuleSettingsControl>("classisland.lessons.timeState", "当前时间状态是", "\uE4C4");
                services.AddRule<CurrentWeatherRuleSettings, CurrentWeatherRuleSettingsControl>("classisland.weather.currentWeather", "当前天气是", "\uE4DC");
                services.AddRule<StringMatchingSettings, RulesetStringMatchingSettingsControl>("classisland.weather.hasWeatherAlert", "存在气象预警", "\uF431");
                services.AddRule<RainTimeRuleSettings, RainTimeRuleSettingsControl>("classisland.weather.rainTime", "距离降水开始/结束还剩", "\uF43F");
                // 行动提供方
                services.AddAction<SignalTriggerSettings, BroadcastSignalActionSettingsControl>("classisland.broadcastSignal", "广播信号", "\uE561");
                services.AddAction<CurrentComponentConfigActionSettings, CurrentComponentConfigActionSettingsControl>("classisland.settings.currentComponentConfig", "组件配置方案", "\ue06f", "应用设置");
                services.AddAction<ThemeActionSettings, ThemeActionSettingsControl>("classisland.settings.theme", "应用主题", "\uE5CB", "应用设置");
                services.AddAction<WindowDockingLocationActionSettings, WindowDockingLocationActionSettingsControl>("classisland.settings.windowDockingLocation", "窗口停靠位置", "\uf397", "应用设置");
                services.AddAction<WindowLayerActionSettings, WindowLayerActionSettingsControl>("classisland.settings.windowLayer", "窗口层级", "\uea2f", "应用设置");
                services.AddAction<WindowDockingOffsetXActionSettings, WindowDockingOffsetXActionSettingsControl>("classisland.settings.windowDockingOffsetX", "窗口向右偏移", "\ue099", "应用设置");
                services.AddAction<WindowDockingOffsetYActionSettings, WindowDockingOffsetYActionSettingsControl>("classisland.settings.windowDockingOffsetY", "窗口向下偏移", "\ue094", "应用设置");
                services.AddAction<RunAction, RunActionSettingsControl>();
                services.AddAction<NotificationAction, NotificationActionSettingsControl>();
                services.AddAction<SleepAction, SleepActionSettingsControl>();
                services.AddAction<SettingsAction, SettingsActionSettingsControl>();
                services.AddAction<WeatherNotificationAction, WeatherNotificationActionSettingControl>();
                services.AddAction<AppQuitAction>();
                services.AddAction<AppRestartAction,AppRestartActionSettingsControl>();
                services.AddHostedService<AppSettingsActionHandler>();

                // 认证提供方
                services.AddAuthorizeProvider<PasswordAuthorizeProvider>();
                // 语音提供方
                services.AddSpeechProvider<SystemSpeechService>();
                services.AddSpeechProvider<EdgeTtsService, EdgeTtsSpeechServiceSettingsControl>();
                services.AddSpeechProvider<GptSoVitsService, GptSovitsSpeechServiceSettingsControl>();
                // 天气图标模板
                // var materialDesignWeatherIconTemplateDictionary = new ResourceDictionary()
                // {
                //     Source = new Uri("avares://ClassIsland/Controls/WeatherIcons/MaterialDesignWeatherIconTemplate.xaml")
                // };
                // services.AddWeatherIconTemplate("classisland.weatherIcons.materialDesign", "Material Design（默认）",
                //     (DataTemplate)materialDesignWeatherIconTemplateDictionary["MaterialDesignWeatherIconTemplate"]!);
                services.AddWeatherIconTemplate("classisland.weatherIcons.lucide", "Lucide（默认）",
                    (this.FindResource("LucideWeatherIconTemplate") as IDataTemplate)!);
                services.AddWeatherIconTemplate("classisland.weatherIcons.fluentDesign", "Fluent Design",
                    (this.FindResource("FluentDesignWeatherIconTemplate") as IDataTemplate)!);
                services.AddWeatherIconTemplate("classisland.weatherIcons.simpleText", "纯文本",
                    (this.FindResource("SimpleTextWeatherIconTemplate") as IDataTemplate)!);
                // Themes
                services.AddXamlTheme(new Uri("avares://ClassIsland/XamlThemes/ClassicTheme/Styles.axaml"),
                    new ThemeManifest()
                    {
                        Id = "classisland.classic",
                        Name = "经典",
                        Description = "ClassIsland 的经典外观。",
                        Banner = "avares://ClassIsland/Assets/XamlThemePreviews/classisland.classic.png",
                        Author = "ClassIsland",
                        Url = "https://github.com/ClassIsland/ClassIsland"
                    });
                services.AddXamlTheme(new Uri("avares://ClassIsland/XamlThemes/FluentTheme/Styles.axaml"),
                    new ThemeManifest()
                    {
                        Id = "classisland.fluent",
                        Name = "Fluent",
                        Description = "焕然一新的 ClassIsland 外观。",
                        Banner = "avares://ClassIsland/Assets/XamlThemePreviews/classisland.fluent.png",
                        Author = "ClassIsland",
                        Url = "https://github.com/ClassIsland/ClassIsland"
                    });
                // Plugins
                if (!ApplicationCommand.Safe)
                {
                    PluginService.InitializePlugins(context, services);
                }
            }).Build();
        AppBase.CurrentLifetime = ClassIsland.Core.Enums.ApplicationLifetime.StartingOffline;
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
        if (!string.IsNullOrWhiteSpace(ApplicationCommand.ImportV1))
        {
            var dtWindow = new DataTransferWindow()
            {
                ImportName = "ClassIsland"
            };
            dtWindow.Show();
            var entries = int.TryParse(ApplicationCommand.ImportEntries, out var r) ? r : 0;
            await dtWindow.PerformClassIslandImport(ApplicationCommand.ImportV1, (ImportEntries)entries);
        }
        var spanLaunching = transaction.StartChild("startup-launching");
        var spanSetupMgmt = spanLaunching.StartChild("startup-setup-mgmt");
        await GetService<IManagementService>().SetupManagement();
        spanSetupMgmt.Finish();
        var spanLoadingSettings = spanLaunching.StartChild("startup-loading-settings");
        await GetService<SettingsService>().LoadSettingsAsync();
        Settings = GetService<SettingsService>().Settings;
        Settings.IsSystemSpeechSystemExist = isSystemSpeechSystemExist;
        Settings.DiagnosticStartupCount++;
        // 记录MLE
        if (ApplicationCommand.PrevSessionMemoryKilled)
        {
            Settings.DiagnosticMemoryKillCount++;
            Settings.DiagnosticLastMemoryKillTime = DateTime.Now;
        }
        spanLoadingSettings.Finish();
        //OverrideFocusVisualStyle();

        CurrentLifetime = Core.Enums.ApplicationLifetime.StartingOnline;
        Logger.LogInformation("初始化应用。");

        // 提前初始化好音频服务，防止在其他地方出现重复初始化的问题
        IAppHost.GetService<IAudioService>();

        IThemeService.IsTransientDisabled = Settings.AnimationLevel < 1;
        IThemeService.IsWaitForTransientDisabled = Settings.IsWaitForTransientDisabled;
        IThemeService.AnimationLevel = Settings.AnimationLevel;
        GetService<ISplashService>().CurrentProgress = 30;
        GetService<ISplashService>().SetDetailedStatus("正在启动挂起检查服务");

        var spanStartHangService = spanLaunching.StartChild("startup-start-hang-service");
        GetService<IHangService>();
        spanStartHangService.Finish();

        GetService<ISplashService>().SetDetailedStatus("正在创建任务栏图标");
        var spanCreateTaskbarIcon = spanLaunching.StartChild("startup-create-taskbar-icon");

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
        // _ = GetService<WallpaperPickingService>().GetWallpaperAsync();
        _ = IAppHost.Host.StartAsync();
        IAppHost.GetService<IPluginMarketService>().LoadPluginSource();

        if (!Settings.IsWelcomeWindowShowed)
        {
            if (Settings.IsSplashEnabled)
            {
                GetService<ISplashService>().EndSplash();
            }
            var w = IAppHost.GetService<WelcomeWindow>();
            await w.ShowDialog(PhonyRootWindow);
            if (!w.ViewModel.IsWizardCompleted)
            {
                Stop();
            }
            else
            {
                Settings.IsWelcomeWindowShowed = true;
                Restart();
            }
            return;
        }

        // 如果不是开发构建, 则自动重置部分可能影响使用的调试选项
        #if !DEBUG
        Settings.IsMainWindowDebugEnabled = false;
        #endif
        
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
            GetService<IAutomationService>().Initialize();
            GetService<IRulesetService>().NotifyStatusChanged();
            File.Delete(startupCountFilePath);
            if (ConfigureFileHelper.Errors.FirstOrDefault(x => x.Critical) != null)
            {
                PlatformServices.DesktopToastService.ShowToastAsync("配置文件损坏", "ClassIsland 部分配置文件已损坏且无法加载，这些配置文件已恢复至默认值。点击此消息以查看详细信息和从过往备份中恢复配置文件。", () => GetService<IUriNavigationService>().NavigateWrapped(new Uri("classisland://app/config-errors")));
            }
            if (Settings.CorruptPluginsDisabledLastSession)
            {
                Settings.CorruptPluginsDisabledLastSession = false;
                var content = new DesktopToastContent()
                {
                    Title = "已自动禁用异常插件",
                    Body = "ClassIsland 已自动禁用导致上次崩溃的插件。您可以在排除问题后前往【应用设置】->【插件】中重新启用这些插件，或在【应用设置】->【基本】中调整是否自动禁用异常插件。",
                    Buttons =
                    {
                        {
                            "打开插件设置",
                            () => GetService<IUriNavigationService>()
                                .NavigateWrapped(new Uri("classisland://app/settings/classisland.plugins"))
                        },
                        {
                            "管理异常插件行为",
                            () => GetService<IUriNavigationService>()
                                .NavigateWrapped(new Uri("classisland://app/settings/general"))
                        }
                    }
                };
                content.Activated += (_, _) => GetService<IUriNavigationService>()
                    .NavigateWrapped(new Uri("classisland://app/settings/classisland.plugins"));
                PlatformServices.DesktopToastService.ShowToastAsync(content);
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
            AppBase.CurrentLifetime = ClassIsland.Core.Enums.ApplicationLifetime.Running;
            if (ApplicationCommand.ImportComplete)
            {
                var dtWindow = new DataTransferWindow()
                {
                    ImportName = "ClassIsland"
                };
                dtWindow.Show();
                dtWindow.ImportComplete();
            }
        };
#if DEBUG
        MemoryProfiler.GetSnapshot("Pre MainWindow show");
#endif
        GetService<ISplashService>().CurrentProgress = 80;
        GetService<ISplashService>().SetDetailedStatus("正在初始化主界面（步骤 2/2）");
        if (!Design.IsDesignMode)
        {
            GetService<MainWindow>().Show();
        }
        GetService<IWindowRuleService>();
        GetService<SignalTriggerHandlerService>();

        // 注册uri导航
        var uriNavigationService = GetService<IUriNavigationService>();
        uriNavigationService.HandleAppNavigation("test", args => _ = CommonTaskDialogs.ShowDialog("测试导航", $"{args.Uri}"));
        uriNavigationService.HandleAppNavigation("settings", args => GetService<SettingsWindowNew>().OpenUri(args.Uri));
        uriNavigationService.HandleAppNavigation("profile", args => GetService<MainWindow>().OpenProfileSettingsWindow());
        uriNavigationService.HandleAppNavigation("helps", args => uriNavigationService.Navigate(new Uri("https://docs.classisland.tech/app/")));
        // uriNavigationService.HandleAppNavigation("profile/import-excel", args => GetService<ExcelImportWindow>().Show());
        // uriNavigationService.HandleAppNavigation("config-errors", args => GetService<ConfigErrorsWindow>().ShowDialog());

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
            var content = new DesktopToastContent()
            {
                Title = "更新完成。",
                Body = $"应用已更新到版本{AppVersion}。",
                Buttons =
                {
                    {
                        "查看更新日志",
                        () => uriNavigationService.NavigateWrapped(new Uri("classisland://app/settings/update"))
                    }
                }
            };
            content.Activated += (_, _) =>
                uriNavigationService.NavigateWrapped(new Uri("classisland://app/settings/update"));
            await PlatformServices.DesktopToastService.ShowToastAsync(content);
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

    // private void UriNavigationCommandExecuted(object sender, ExecutedRoutedEventArgs e)
    // {
    //     var uri = "";
    //     if (e.Parameter is string uriRaw)
    //     {
    //         uri = uriRaw;
    //     }
    //
    //     //if (sender is Hyperlink hyperlink)
    //     //{
    //     //    uri = hyperlink.GetHref();
    //     //}
    //     try
    //     {
    //         IAppHost.GetService<IUriNavigationService>().NavigateWrapped(new Uri(uri));
    //     }
    //     catch (Exception ex)
    //     {
    //         Logger?.LogError(ex, "无法导航到 {}", uri);
    //         CommonDialog.ShowError($"无法导航到 {uri}：{ex.Message}");
    //     }
    // }

    private async Task ProcessInstanceExisted()
    {
        var dialog = new TaskDialog()
        {
            Title = "ClassIsland 已在运行",
            Content = "ClassIsland 已经启动，请通过任务栏托盘图标进行设置等操作。\n\n" +
                      "如果您无法看到主界面，可能是因为您在托盘图标菜单中选择了【隐藏主界面】，或者有隐藏主界面的规则或行动正在生效。",
            XamlRoot = GetRootWindow(),
            Buttons =
            [
                new TaskDialogButton("取消", false)
            ],
            Commands =
            [
                new TaskDialogCommand()
                {
                    DialogResult = true,
                    ClosesOnInvoked = true,
                    Text = "重启当前实例",
                    Description = "结束正在运行的 ClassIsland 实例，然后再次启动本实例。",
                    IconSource = new FluentIconSource("\ue0bd"),
                }
            ]
        };
        var r = await dialog.ShowAsync();
        if (!Equals(r, true))
        {
            return;
        }
        try
        {
            var proc = Process.GetProcessesByName(System.OperatingSystem.IsWindows()
                ? Path.GetFileNameWithoutExtension(Environment.ProcessPath)
                : Environment.ProcessPath?.Replace(".dll", ""))
                .Where(x=>x.Id != Environment.ProcessId);
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
            await CommonTaskDialogs.ShowDialog("重启失败", $"无法重新启动应用，可能当前运行的实例正在以管理员身份运行。请使用任务管理器终止正在运行的实例，然后再试一次。\n\n{e.Message}");
        }
    }

    public static void ReleaseLock()
    {
        var app = (App)Application.Current!;
        app.Mutex?.ReleaseMutex();
    }

    public override void Stop()
    {
        if (CurrentLifetime == ClassIsland.Core.Enums.ApplicationLifetime.Stopping)
        {
            return;
        }
        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            var partial = CurrentLifetime < Core.Enums.ApplicationLifetime.StartingOnline;
            CurrentLifetime = ClassIsland.Core.Enums.ApplicationLifetime.Stopping;
            Logger?.LogInformation("正在停止应用");
            if (IAppHost.TryGetService<IManagementService>() is { IsManagementEnabled: true, Connection: ManagementServerConnection connection })
            {
                connection.LogAuditEvent(AuditEvents.AppExited, new Empty());
            }
            AppStopping?.Invoke(this, EventArgs.Empty);
            if (!partial)
            {
                IAppHost.Host?.Services.GetService<ILessonsService>()?.StopMainTimer();
                IAppHost.Host?.StopAsync(TimeSpan.FromSeconds(5));
                IAppHost.Host?.Services.GetService<SettingsService>()?.SaveSettings("停止当前应用程序。");
                IAppHost.Host?.Services.GetService<IAutomationService>()?.SaveConfig("停止当前应用程序。");
                IAppHost.Host?.Services.GetService<IProfileService>()?.SaveProfile();
                IAppHost.Host?.Services.GetService<IComponentsService>()?.SaveConfig();
            }
            DesktopLifetime?.Shutdown();
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
        var replaced = path.Replace(".dll", PlatformExecutableExtension);
        var startInfo = new ProcessStartInfo(replaced);
        foreach (var i in parameters)
        {
            startInfo.ArgumentList.Add(i);
        }
        Process.Start(startInfo);
    }

    private void NativeMenuItemOpenAbout_OnClick(object? sender, EventArgs e)
    {
        if (CurrentLifetime != Core.Enums.ApplicationLifetime.Running || !Settings.IsWelcomeWindowShowed)
        {
            return;
        }
        IAppHost.GetService<SettingsWindowNew>().Open("about");
    }

    private void NativeMenuItemExit_OnClick(object? sender, EventArgs e)
    {
        Stop();
    }
}


