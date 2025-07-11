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
using Avalonia.Controls;
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
        if (Design.IsDesignMode)
        {
            // 设计时不加载设置
            Logger.LogInformation("检测到应用以设计模式运行，跳过配置加载");
            return;
        }
        try
        {
            if (!File.Exists(Path.Combine(CommonDirectories.AppRootFolderPath, "Settings.json")))
            {
                SkipMigration = true; // 如果是新的配置文件，那么就需要跳过迁移。
                Logger.LogInformation("配置文件不存在，跳过加载。");
            }
            else
            {
                Logger.LogInformation("加载配置文件。");
                var r = ConfigureFileHelper.LoadConfig<Settings>(Path.Combine(CommonDirectories.AppRootFolderPath, "Settings.json"));
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
        
    }
#pragma warning restore CS0612 // 类型或成员已过时

    public void SaveSettings(string note = "")
    {
        if (Design.IsDesignMode)
        {
            return;
        }
        Logger.LogInformation(note == "" ? "写入配置文件。" : $"写入配置文件：{note}");
        ConfigureFileHelper.SaveConfig(Path.Combine(CommonDirectories.AppRootFolderPath, "Settings.json"), Settings);
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