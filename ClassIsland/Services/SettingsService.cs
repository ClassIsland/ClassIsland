using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.Json.Serialization;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Services.Management;
using ClassIsland.Shared.Protobuf.AuditEvent;
using ClassIsland.Shared.Protobuf.Enum;
using Edge_tts_sharp.Model;
using Octokit;

namespace ClassIsland.Services;

public class SettingsService(ILogger<SettingsService> Logger, IManagementService ManagementService)
    : INotifyPropertyChanged
{
    private Settings _settings = new();

    private bool SkipMigration { get; set; } = false;

    public Settings Settings
    {
        get => _settings;
        set => SetField(ref _settings, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool WillMigrateProfileTrustedState { get; set; } = false;

    private async Task LoadManagementSettingsAsync()
    {
        if (!ManagementService.Manifest.DefaultSettingsSource.IsNewerAndNotNull(ManagementService.Versions
                .DefaultSettingsVersion) ||
            ManagementService.Connection == null)
        {
            return;
        }

        Logger.LogInformation("拉取集控默认设置");
        var url = ManagementService.Manifest.DefaultSettingsSource.Value!;
        var settings = await ManagementService.Connection.GetJsonAsync<Settings>(url);
        Settings = settings;
        Settings.PropertyChanged += (sender, args) => SettingsChanged(args.PropertyName!);
        Logger.LogTrace("拉取集控默认设置成功！");
    }

    public async Task LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists(Path.Combine(App.AppRootFolderPath, "Settings.json")))
            {
                SkipMigration = true; // 如果是新的配置文件，那么就需要跳过迁移。
                Logger.LogInformation("配置文件不存在，跳过加载。");
            }
            else
            {
                Logger.LogInformation("加载配置文件。");
                var r = ConfigureFileHelper.LoadConfig<Settings>(Path.Combine(App.AppRootFolderPath, "Settings.json"));
                Settings = r;
                Settings.PropertyChanged += (sender, args) => SettingsChanged(args.PropertyName!);
            }

            // 当还没有初始化应用且启用集控时，从集控拉取设置。
            if (ManagementService.IsManagementEnabled && !Settings.IsWelcomeWindowShowed)
            {
                await LoadManagementSettingsAsync();
            }

            ISpeechService.GlobalSettings = Settings;
        }
        catch (Exception ex)
        {
            SkipMigration = true;
            Logger.LogError(ex, "配置文件加载失败。");
            // ignored
        }

        if (Settings is { IsSystemSpeechSystemExist: false, SpeechSource: 0 })
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

    private T TryGetDictionaryValue<T>(IDictionary<string, object?> dictionary, string key, T? fallbackValue = null)
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

#pragma warning disable CS0612 // 类型或成员已过时
    private void MigrateSettings(out bool requiresRestarting)
    {
        requiresRestarting = false;
        if (Assembly.GetExecutingAssembly().GetName().Version < Version.Parse("1.4.1.0"))
        {
            return;
        }
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

        // 旧城市Id 迁移到 新城市Id
        if (int.TryParse(Settings.CityId, out var oldCityId))
        {
            Settings.CityId = $"weathercn:{oldCityId}";
            Logger.LogInformation("新格式城市Id转换完成！");
        }
        
        if (Settings.LastAppVersion < Version.Parse("1.5.3.0"))
        {
            Settings.SelectedUpdateChannelV2 = Settings.SelectedChannel switch
            {
                "https://install.appcenter.ms/api/v0.1/apps/hellowrc/classisland/distribution_groups/public" =>
                    "stable",
                "https://install.appcenter.ms/api/v0.1/apps/hellowrc/classisland/distribution_groups/publicbeta" => "beta",
                _ => "stable"
            };
            Logger.LogInformation("成功迁移了 1.5.3.0 以前的设置。");
        }
        if (Settings.LastAppVersion < Version.Parse("1.5.4.0"))
        {
            WillMigrateProfileTrustedState = true;
            Logger.LogInformation("成功迁移了 1.5.4.0 以前的设置。");
        }
        if (Settings.LastAppVersion < Version.Parse("1.6.3.0"))
        {
            Settings.SelectedSpeechProvider = Settings.SpeechSource switch
            {
                0 => "classisland.speech.system",
                1 => "classisland.speech.edgeTts",
                2 => "classisland.speech.gpt-sovits",
                _ => Settings.SelectedSpeechProvider
            };
            Logger.LogInformation("成功迁移了 1.6.3.0 以前的设置。");
        }
    }
#pragma warning restore CS0612 // 类型或成员已过时

    public void SaveSettings(string note = "")
    {
        Logger.LogInformation(note == "" ? "写入配置文件。" : $"写入配置文件：{note}");
        ConfigureFileHelper.SaveConfig(Path.Combine(App.AppRootFolderPath, "Settings.json"), Settings);
    }

    /// <summary>
    /// 添加设置叠层。
    /// </summary>
    /// <param name="guid">叠层Guid</param>
    /// <param name="binding">设置变量名</param>
    public void AddSettingsOverlay(string guid, string binding, dynamic? value)
    {
        var property = typeof(Settings).GetProperty(binding);
        if (property == null) throw new KeyNotFoundException($"找不到设置变量{property}");

        if (!Settings.SettingsOverlay.TryGetValue(binding, out Dictionary<string, dynamic?>? overlay))
        {
            overlay = [];
            var original = property.GetValue(Settings);
            if (value.ToString() == original.ToString()) return;
            overlay["@"] = original;
        }

        property.SetValue(Settings, value);
        overlay[guid] = value;
        Settings.SettingsOverlay[binding] = overlay;
    }

    /// <summary>
    /// 删除设置叠层。
    /// </summary>
    /// <param name="guid">叠层Guid</param>
    /// <param name="binding">设置变量名</param>
    public void RemoveSettingsOverlay(string guid, string binding)
    {
        var property = typeof(Settings).GetProperty(binding);
        if (property == null) throw new KeyNotFoundException($"找不到设置变量{property}");
        if (!Settings.SettingsOverlay.TryGetValue(binding, out Dictionary<string, dynamic?>? overlay)) return;

        overlay.Remove(guid);
        var last = overlay.Last().Value;
        if (last is JsonElement json)
            last = json.Deserialize(property.GetValue(Settings).GetType());

        property.SetValue(Settings, last);

        if (overlay.Count > 1)
            Settings.SettingsOverlay[binding] = overlay;
        else
            Settings.SettingsOverlay.Remove(binding);
    }

    private void SettingsChanged(string propertyName)
    {
        if (propertyName != nameof(Settings.SettingsOverlay))
            Settings.SettingsOverlay.Remove(propertyName);

        if (typeof(Settings).GetProperty(propertyName)
                            .GetCustomAttribute<JsonIgnoreAttribute>() != null)
            return;
        SaveSettings(propertyName);
        if (ManagementService is { IsManagementEnabled: true, Connection: ManagementServerConnection connection })
        {
            connection.LogAuditEvent(AuditEvents.AppSettingsUpdated, new AppSettingsUpdated()
            {
                PropertyName = propertyName
            });
        }
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