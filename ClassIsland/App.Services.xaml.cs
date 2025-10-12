using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ClassIsland.Controls.ActionSettingsControls;
using ClassIsland.Controls.AttachedSettingsControls;
using ClassIsland.Controls.AuthorizeProvider;
using ClassIsland.Controls.Components;
using ClassIsland.Controls.NotificationProviders;
using ClassIsland.Controls.RuleSettingsControls;
using ClassIsland.Controls.SpeechProviderSettingsControls;
using ClassIsland.Controls.TriggerSettingsControls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Abstractions.Services.Metadata;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Core.Controls.Ruleset;
using ClassIsland.Core.Extensions.Registry;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models.Logging;
using ClassIsland.Core.Models.Ruleset;
using ClassIsland.Core.Models.XamlTheme;
using ClassIsland.Models.Actions;
using ClassIsland.Models.Automation.Triggers;
using ClassIsland.Models.Rules;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Services;
using ClassIsland.Services.ActionHandlers;
using ClassIsland.Services.AppUpdating;
using ClassIsland.Services.Automation.Actions;
using ClassIsland.Services.Automation.Triggers;
using ClassIsland.Services.Logging;
using ClassIsland.Services.Management;
using ClassIsland.Services.Metadata;
using ClassIsland.Services.NotificationProviders;
using ClassIsland.Services.SpeechService;
using ClassIsland.ViewModels;
using ClassIsland.ViewModels.SettingsPages;
using ClassIsland.Views;
using ClassIsland.Views.SettingPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace ClassIsland;

public partial class App
{
    private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
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
            builder.AddConsole(console => { console.FormatterName = "classisland"; });
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
        services.AddAction<AppRestartAction, AppRestartActionSettingsControl>();
        services.AddHostedService<AppSettingsActionHandler>();

        // 认证提供方
        services.AddAuthorizeProvider<PasswordAuthorizeProvider>();
        // 语音提供方
        services.AddSpeechProvider<SystemSpeechService>();
        services.AddSpeechProvider<EdgeTtsService, EdgeTtsSpeechServiceSettingsControl>();
        services.AddSpeechProvider<GptSoVitsService, GptSovitsSpeechServiceSettingsControl>();
        // 天气图标模板
        services.AddWeatherIconTemplate("classisland.weatherIcons.lucide", "Lucide（默认）", (this.FindResource("LucideWeatherIconTemplate") as IDataTemplate)!);
        services.AddWeatherIconTemplate("classisland.weatherIcons.fluentDesign", "Fluent Design", (this.FindResource("FluentDesignWeatherIconTemplate") as IDataTemplate)!);
        services.AddWeatherIconTemplate("classisland.weatherIcons.simpleText", "纯文本", (this.FindResource("SimpleTextWeatherIconTemplate") as IDataTemplate)!);
        // Themes
        services.AddXamlTheme(new Uri("avares://ClassIsland/XamlThemes/ClassicTheme/Styles.axaml"), new ThemeManifest()
        {
            Id = "classisland.classic",
            Name = "经典",
            Description = "ClassIsland 的经典外观。",
            Banner = "avares://ClassIsland/Assets/XamlThemePreviews/classisland.classic.png",
            Author = "ClassIsland",
            Url = "https://github.com/ClassIsland/ClassIsland"
        });
        services.AddXamlTheme(new Uri("avares://ClassIsland/XamlThemes/FluentTheme/Styles.axaml"), new ThemeManifest()
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
    }
}