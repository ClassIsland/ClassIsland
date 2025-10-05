using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Shared.Helpers;
using ClassIsland.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Services.Automation.Actions;
using ClassIsland.Services.Management;
using ClassIsland.Shared.Protobuf.AuditEvent;
using ClassIsland.Shared.Protobuf.Enum;

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
        Logger.LogInformation(note == "" ? "写入配置文件。" : "写入配置文件：{}", note);
        ConfigureFileHelper.SaveConfig(Path.Combine(CommonDirectories.AppRootFolderPath, "Settings.json"), Settings);
    }


    public const BindingFlags SettingsPropertiesFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    public static readonly JsonSerializerOptions AllowReadingFromString = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    /// <summary>
    /// 添加应用设置叠层。
    /// </summary>
    /// <param name="guid">设置叠层 Guid。</param>
    /// <param name="name">设置属性名称。</param>
    /// <param name="value">要设置的值。</param>
    /// <returns>是否成功。失败会抛出。</returns>
    public bool AddSettingsOverlay(Guid guid, string name, dynamic value)
    {
        var key = guid.ToString();
        var isPropertyOverlayNotNull = Settings.SettingsOverlays.TryGetValue(name, out var propertyOverlay);
        if (isPropertyOverlayNotNull)
            propertyOverlay.Remove(key);

        var info = typeof(Settings).GetProperty(name, SettingsPropertiesFlags);
        if (info == null)
        {
            throw new KeyNotFoundException($"应用设置中找不到属性“{name}”。");
            return false;
        }

        var type = info.PropertyType;
        if (value.GetType() != type)
            value = SettingsAction.Deserialize(value, type);

        if (!isPropertyOverlayNotNull || propertyOverlay.Contains(key))
        {
            propertyOverlay = [];
            dynamic sourceValue = info.GetValue(Settings);
            if (value == sourceValue)
                return true;
            propertyOverlay["@"] = sourceValue;
        }

        info.SetValue(Settings, value);
        propertyOverlay[key] = value;
        Settings.SettingsOverlays[name] = propertyOverlay;
        return true;
    }

    public static PropertyInfo GetPropertyInfoByName(string name) =>
        typeof(Settings).GetProperty(name, SettingsPropertiesFlags) ??
        throw new KeyNotFoundException($"应用设置中找不到属性“{name}”。");

    /// <summary>
    /// 删除应用设置叠层。
    /// </summary>
    /// <param name="guid">设置叠层 Guid。</param>
    /// <param name="name">设置属性名。</param>
    /// <returns>是否进行了删除。删除出错会抛出。</returns>
    public bool RemoveSettingsOverlay(Guid guid, string name)
    {
        if (!Settings.SettingsOverlays.TryGetValue(name, out var propertyOverlay))
            return false;

        var lengthBefore = propertyOverlay.Count;
        propertyOverlay.Remove(guid.ToString());
        var length = propertyOverlay.Count;
        if (length == lengthBefore)
            return false;

        var info = typeof(Settings).GetProperty(name, SettingsPropertiesFlags);
        if (info == null)
            return false;

        var type = info.PropertyType;
        var last = propertyOverlay[length - 1]; // propertyOverlay 至少存在一项。
        // if (last is JsonElement json)
        //     last = json.Deserialize(type, AllowReadingFromString);
        if (last.GetType() != type)
            last = SettingsAction.Deserialize(last.ToString(), type);

        info.SetValue(Settings, last);

        if (length > 1)
            Settings.SettingsOverlays[name] = propertyOverlay;
        else
            Settings.SettingsOverlays.Remove(name);
        return true;
    }

    void SettingsChanged(string propertyName)
    {
        if (propertyName != nameof(Settings.SettingsOverlays))
        {
            Settings.SettingsOverlays.Remove(propertyName);
        }

        if (typeof(Settings).GetProperty(propertyName, SettingsPropertiesFlags)?
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