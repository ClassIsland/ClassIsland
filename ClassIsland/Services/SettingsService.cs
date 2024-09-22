using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Models.Components;
using ClassIsland.Shared.Helpers;
using ClassIsland.Models;
using ClassIsland.Models.ComponentSettings;
using ClassIsland.Services.Management;

using Microsoft.Extensions.Logging;
using System.Linq;
using ClassIsland.Models.Actions;

namespace ClassIsland.Services;

public class SettingsService : INotifyPropertyChanged
{
    private Settings _settings = new();

    private bool SkipMigration { get; set; } = false;

    public Settings Settings
    {
        get => _settings;
        set => SetField(ref _settings, value);
    }

    private ILogger<SettingsService> Logger { get; }

    private IManagementService ManagementService { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private async Task LoadManagementSettingsAsync()
    {
        if (!ManagementService.Manifest.DefaultSettingsSource.IsNewerAndNotNull(ManagementService.Versions.DefaultSettingsVersion) ||
            ManagementService.Connection == null)
        {
            return;
        }
        
        Logger.LogInformation("拉取集控默认设置");
        var url = ManagementService.Manifest.DefaultSettingsSource.Value!;
        var settings = await ManagementService.Connection.GetJsonAsync<Settings>(url);
        Settings = settings;
        Settings.PropertyChanged += (sender, args) => SaveSettings(args.PropertyName);
        Settings.PropertyChanged += (sender, args) => SettingsChanged(args.PropertyName);
        Logger.LogTrace("拉取集控默认设置成功！");
    }

    public async Task LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists("./Settings.json"))
            {
                SkipMigration = true;  // 如果是新的配置文件，那么就需要跳过迁移。
                Logger.LogInformation("配置文件不存在，跳过加载。");
            }
            else
            {
                Logger.LogInformation("加载配置文件。");
                var r = ConfigureFileHelper.LoadConfig<Settings>("./Settings.json");
                Settings = r;
                Settings.PropertyChanged += (sender, args) => SaveSettings(args.PropertyName);
                Settings.PropertyChanged += (sender, args) => SettingsChanged(args.PropertyName);
            }

            // 当还没有初始化应用且启用集控时，从集控拉取设置。
            if (ManagementService.IsManagementEnabled && !Settings.IsWelcomeWindowShowed)
            {
                await LoadManagementSettingsAsync();
            }
        }
        catch(Exception ex)
        {
            SkipMigration = true;
            Logger.LogError(ex, "配置文件加载失败。");
            // ignored
        }
        if (!Settings.IsSystemSpeechSystemExist)
        {
            Settings.SpeechSource = 1;
        }

        var requiresRestarting = false;
        if (!SkipMigration)
        {
            MigrateSettings(out requiresRestarting);
        }

        Settings.LastAppVersion = Assembly.GetExecutingAssembly().GetName().Version!;

        if (requiresRestarting)
        {
            AppBase.Current.Restart();
        }
    }

    private T TryGetDictionaryValue<T>(IDictionary<string, object?> dictionary, string key, T? fallbackValue=null)
        where T : class
    {
        var fallback = fallbackValue ?? Activator.CreateInstance<T>();
        var r = Settings.MiniInfoProviderSettings.TryGetValue(key.ToLower(), out var o);
        if (o is JsonElement o1)
        {
            return o1.Deserialize<T>() ?? fallback;
        }
        return (T?)Settings.MiniInfoProviderSettings[key.ToLower()] ?? fallback;
    }

    private void MigrateSettings(out bool requiresRestarting)
    {
        requiresRestarting = false;
        if (Settings.LastAppVersion < Version.Parse("1.4.1.0"))  // 从 1.4.1.0 以前的版本升级
        {
            var componentsService = App.GetService<IComponentsService>();
            componentsService.CurrentComponents.Clear();
            var island = componentsService.CurrentComponents;
            var miniInfo = Settings.SelectedMiniInfoProvider?.ToUpper() switch
            {
                // 日期
                "D9FC55D6-8061-4C21-B521-6B0532FF735F" => new ComponentSettings
                    { Id = "DF3F8295-21F6-482E-BADA-FA0E5F14BB66" },
                // 天气简报 
                "EA336289-5A60-49EF-AD36-858109F37644" => new ComponentSettings
                {
                    Id = "CA495086-E297-4BEB-9603-C5C1C1A8551E",
                    Settings = new WeatherComponentSettings()
                    {
                        ShowAlerts = TryGetDictionaryValue(Settings.MiniInfoProviderSettings,
                                "EA336289-5A60-49EF-AD36-858109F37644", new WeatherMiniInfoProviderSettings())
                            .IsAlertEnabled
                    }
                },
                // 倒计时日 
                "DE09B49D-FE61-11EE-9DF4-43208C458CC8" => new ComponentSettings
                {
                    Id = "7C645D35-8151-48BA-B4AC-15017460D994",
                    Settings = new CountDownComponentSettings()
                    {
                        CountDownName =
                            TryGetDictionaryValue<CountDownMiniInfoProviderSettings>(Settings.MiniInfoProviderSettings,
                                "DE09B49D-FE61-11EE-9DF4-43208C458CC8").countDownName,
                        FontColor = TryGetDictionaryValue<CountDownMiniInfoProviderSettings>(
                            Settings.MiniInfoProviderSettings, "DE09B49D-FE61-11EE-9DF4-43208C458CC8").fontColor,
                        FontSize = TryGetDictionaryValue<CountDownMiniInfoProviderSettings>(
                            Settings.MiniInfoProviderSettings, "DE09B49D-FE61-11EE-9DF4-43208C458CC8").fontSize,
                        OverTime = TryGetDictionaryValue<CountDownMiniInfoProviderSettings>(
                            Settings.MiniInfoProviderSettings, "DE09B49D-FE61-11EE-9DF4-43208C458CC8").overTime
                    }
                },
                _ => new ComponentSettings { Id = "DF3F8295-21F6-482E-BADA-FA0E5F14BB66" }
            };
            if (Settings.ShowDate)
            {
                island.Add(miniInfo);
            }

            island.Add(new ComponentSettings()
            {
                Id = "1DB2017D-E374-4BC6-9D57-0B4ADF03A6B8",
                Settings = new LessonControlSettings()
                {
                    CountdownSeconds = Settings.CountdownSeconds,
                    ExtraInfoType = Settings.ExtraInfoType,
                    IsCountdownEnabled = Settings.IsCountdownEnabled,
                    ShowExtraInfoOnTimePoint = Settings.ShowExtraInfoOnTimePoint
                }
            });
            Settings.ShowComponentsMigrateTip = true;
            Settings.IsMigratedFromv1_4 = true;
            Logger.LogInformation("成功迁移了 1.4.1.0 以前的设置。");
        }

        if (Settings.LastAppVersion < Version.Parse("1.4.3.0"))
        {
            Settings.IsSentryEnabled = Settings.IsReportingEnabled;
            requiresRestarting = true;
            Logger.LogInformation("成功迁移了 1.4.3.0 以前的设置。");
        }

    }

    public void SaveSettings(string note = "-")
    {
        Logger.LogInformation("写入配置文件：" + note);
        ConfigureFileHelper.SaveConfig("./Settings.json", Settings);
    }

    /// <summary>
    /// 添加设置叠层。
    /// </summary>
    /// <param name="id">叠层Guid</param>
    /// <param name="name">设置变量名</param>
    public void AddSettingsOverlay(string id, string name, dynamic value)
    {
        var property = typeof(Settings).GetProperty(name);
        if (property == null) throw new KeyNotFoundException($"找不到设置变量{property}");

        if (!Settings.SettingsOverlay.TryGetValue(name, out Dictionary<string, dynamic>? overlay))
            overlay = [];
        if (!overlay.ContainsKey("@"))
            overlay.Add("@", property.GetValue(Settings)!);
        property.SetValue(Settings, value);
        overlay[id] = value;
        Settings.SettingsOverlay[name] = overlay;
    }

    /// <summary>
    /// 删除设置叠层。
    /// </summary>
    /// <param name="id">叠层Guid</param>
    /// <param name="name">设置变量名</param>
    public void RemoveSettingsOverlay(string id, string name)
    {
        var property = typeof(Settings).GetProperty(name);
        if (property == null) throw new KeyNotFoundException($"找不到设置变量{property}");

        if (!Settings.SettingsOverlay.TryGetValue(name, out Dictionary<string, dynamic>? overlay))
            return;
        overlay.Remove(id);
        var last = overlay.Last();
        var value = last.Value;
        if (value is JsonElement json)
        {
            value = json.Deserialize(property.GetValue(Settings).GetType());
        }
        property.SetValue(Settings, value);
        Settings.SettingsOverlay[name] = overlay;

        if (last.Key is "@")
            Settings.SettingsOverlay.Remove(name);
    }

    public SettingsService(ILogger<SettingsService> logger, IManagementService managementService)
    {
        Logger = logger;
        ManagementService = managementService;

        var actionService = App.GetService<IActionService>();
        actionService.RegisterActionHandler("classisland.settings.currentComponentConfig", s => AddSettingsOverlay("classisland.settings.currentComponentConfig", "CurrentComponentConfig", ((CurrentComponentConfigActionSettings)s!).Value));
        actionService.RegisterActionBackHandler("classisland.settings.currentComponentConfig", s => RemoveSettingsOverlay("classisland.settings.currentComponentConfig", "CurrentComponentConfig"));
        actionService.RegisterActionHandler("classisland.settings.theme", s => AddSettingsOverlay("classisland.settings.theme", "Theme", ((ThemeActionSettings)s!).Value));
        actionService.RegisterActionBackHandler("classisland.settings.theme", s => RemoveSettingsOverlay("classisland.settings.theme", "Theme"));
        actionService.RegisterActionHandler("classisland.settings.windowDockingLocation", s => AddSettingsOverlay("classisland.settings.windowDockingLocation", "WindowDockingLocation", ((WindowDockingLocationActionSettings)s!).Value));
        actionService.RegisterActionBackHandler("classisland.settings.windowDockingLocation", s => RemoveSettingsOverlay("classisland.settings.windowDockingLocation", "WindowDockingLocation"));
    }

    private void SettingsChanged(string propertyName)
    {
        if (propertyName is "SettingsOverlay") return;
        Settings.SettingsOverlay.Remove(propertyName);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}