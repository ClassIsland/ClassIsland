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
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Models.Components;
using ClassIsland.Shared.Helpers;
using ClassIsland.Models;
using ClassIsland.Models.ComponentSettings;
using ClassIsland.Services.Management;

using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class SettingsService(ILogger<SettingsService> logger, IManagementService managementService) : INotifyPropertyChanged
{
    private Settings _settings = new();

    private bool SkipMigration { get; set; } = false;

    public Settings Settings
    {
        get => _settings;
        set => SetField(ref _settings, value);
    }

    private ILogger<SettingsService> Logger { get; } = logger;

    private IManagementService ManagementService { get; } = managementService;

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
        Settings.PropertyChanged += (sender, args) => SaveSettings();
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
                Settings.PropertyChanged += (sender, args) => SaveSettings();
            }

            // 当还没有初始化应用且启用集控时，从集控拉取设置。
            if (ManagementService.IsManagementEnabled && !Settings.IsWelcomeWindowShowed)
            {
                await LoadManagementSettingsAsync();
                SkipMigration = false;  // 从集控拉取的默认配置不需要跳过迁移。
            }
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "配置文件加载失败。");
            // ignored
        }
        if (!Settings.IsSystemSpeechSystemExist)
        {
            Settings.SpeechSource = 1;
        }

        if (!SkipMigration)
        {
            MigrateSettings();
        }

        Settings.LastAppVersion = Assembly.GetExecutingAssembly().GetName().Version!;
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

    private void MigrateSettings()
    {
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
                Id = "E7831603-61A0-4180-B51B-54AD75B1A4D3"
            });
            Settings.ShowComponentsMigrateTip = true;
            Logger.LogInformation("成功迁移了 1.4.1.0 以前的设置。");
        }
    }

    public void SaveSettings()
    {
        Logger.LogInformation("写入配置文件。");
        ConfigureFileHelper.SaveConfig("./Settings.json", Settings);
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