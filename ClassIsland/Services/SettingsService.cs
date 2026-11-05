using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Shared.Helpers;
using ClassIsland.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Helpers;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Services.Automation.Actions;
using ClassIsland.Services.Management;
using ClassIsland.Shared.Protobuf.AuditEvent;
using ClassIsland.Shared.Protobuf.Enum;

namespace ClassIsland.Services;

public class SettingsService(ILogger<SettingsService> Logger, IManagementService ManagementService)
    : INotifyPropertyChanged, IDisposable
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

    public bool WillMigrateInitTutorial { get; set; } = false;

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

        // 只有到这里才允许写回：在此之前进程若退出/重启（如 --importV1 中途），
        // FlushPendingSave 不会把尚未加载完成的（默认）设置刷进 Settings.json。
        _settingsLoaded = true;

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
        
        if (Settings.LastAppVersion < Version.Parse("2.0.0.2") 
            || (Settings.LastAppVersion >= Version.Parse("2.0.1.0") && Settings.LastAppVersion < Version.Parse("2.0.1.2")) )
        {
            if (PlatformServices.DesktopService.IsUrlSchemeRegistered)
            {
                PlatformServices.DesktopService.IsUrlSchemeRegistered = true;
            }
            Logger.LogInformation("成功迁移了 2.0.0.2 以前的设置。");
        }
        
        if (Settings.LastAppVersion < Version.Parse("2.0.3.0") )
        {
            WillMigrateInitTutorial = true;
            Logger.LogInformation("成功迁移了 2.0.3.0 以前的设置。");
        }

        if (Settings.LastAppVersion < Version.Parse("2.0.3.3"))
        {
            if (PlatformServices.DesktopService.IsAutoStartEnabled)
            {
                PlatformServices.DesktopService.IsAutoStartEnabled = false;
                PlatformServices.DesktopService.IsAutoStartEnabled = true;
            }
            Logger.LogInformation("成功迁移了 2.0.3.3 以前的自启动快捷方式。");
        }
        
    }
#pragma warning restore CS0612 // 类型或成员已过时

    public void SaveSettings(string note = "")
    {
        SaveSettingsCore(note);
    }

    /// <summary>
    /// 写盘的统一实现。所有 Settings.json 写入（显式保存与延迟合并写入）都经由此方法，
    /// 在 <see cref="_saveWriteLock"/> 内单飞执行。
    /// </summary>
    /// <remarks>设置尚未加载完成（如启动早期、--import 中途）时拒绝写回，防止默认值覆盖配置文件。</remarks>
    private void SaveSettingsCore(string note = "")
    {
        if (Design.IsDesignMode)
        {
            return;
        }
        if (!_settingsLoaded)
        {
            Logger.LogDebug("设置尚未加载完成，跳过写入配置文件。");
            return;
        }
        lock (_saveWriteLock)
        {
            Logger.LogInformation(note == "" ? "写入配置文件。" : "写入配置文件：{}", note);
            ConfigureFileHelper.SaveConfig(Path.Combine(CommonDirectories.AppRootFolderPath, "Settings.json"), Settings);
        }
    }

    /// <summary>
    /// 设置写入的合并窗口。
    /// </summary>
    /// <remarks>
    /// 旧实现里每一次设置项变更都会**同步**完成一次 Settings.json 的完整序列化 + 落盘，
    /// 而这个回调运行在发起修改的线程（通常是 UI 线程）上。
    /// 只要有一个设置项被高频写入（动画参数、音量、鼠标状态、调试时间偏移等），
    /// 就会变成每秒几十次的整文件重写；运行时间越长、Settings.json 越大，单次写入越慢，
    /// 最终把 UI 线程彻底堵死，表现为"用久了就卡死"。
    /// 这里把连续的变更合并成一次写入；显式调用 <see cref="SaveSettings"/> 仍然立即落盘。
    /// </remarks>
    private const int SaveDebounceMilliseconds = 800;

    private readonly object _pendingSaveLock = new();
    /// <summary>
    /// 序列化 + 落盘的单一关口：无论写入来自定时器回调（<see cref="FlushPendingSave"/>）
    /// 还是显式的 <see cref="SaveSettings"/>，文件写入本身只允许一票进入，
    /// 避免 <c>FileShare.None</c> 下的并发写冲突（两个写线程只有一个能成功）。
    /// </summary>
    private readonly object _saveWriteLock = new();
    private Timer? _saveTimer;
    private string _pendingSaveNote = "";
    /// <summary>是否有尚未落盘的合并写入（定时器 armed 或尚未触发）。</summary>
    private bool _hasPendingSave;
    /// <summary>
    /// <see cref="LoadSettingsAsync"/> 是否已经完成。在它完成前禁止任何写回：
    /// 否则进程在导入/迁移场景中途退出时，会把**默认值**当作用户设置写进
    /// Settings.json，覆盖刚导入或已存在的配置。
    /// </summary>
    private volatile bool _settingsLoaded;
    private bool _disposed;

    private void ScheduleSave(string note)
    {
        if (Design.IsDesignMode || _disposed)
        {
            return;
        }

        lock (_pendingSaveLock)
        {
            _pendingSaveNote = note;
            _hasPendingSave = true;
            _saveTimer ??= new Timer(_ => FlushPendingSave(), null, Timeout.Infinite, Timeout.Infinite);
            _saveTimer.Change(SaveDebounceMilliseconds, Timeout.Infinite);
        }
    }

    /// <summary>
    /// 立即落盘尚未写入的设置变更。应用退出或重启前应调用一次。
    /// 没有待写入的变更、或设置尚未加载完成时，本方法是空操作（不会把默认值刷盘）。
    /// </summary>
    public void FlushPendingSave()
    {
        string note;
        lock (_pendingSaveLock)
        {
            if (!_hasPendingSave)
            {
                return;
            }
            note = _pendingSaveNote;
            _pendingSaveNote = "";
            _hasPendingSave = false;
            // 取消定时器：接下来的写入就是这次 flush 本身，防止 timer 再触发一次重复写。
            _saveTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        try
        {
            SaveSettingsCore(note);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "延迟写入配置文件失败。");
            // 写失败不能吞掉变更：恢复 pending 并重新武装定时器，
            // 否则这次的设置变更会被静默丢弃。
            lock (_pendingSaveLock)
            {
                _hasPendingSave = true;
                _pendingSaveNote = note;
                _saveTimer?.Change(SaveDebounceMilliseconds, Timeout.Infinite);
            }
        }
    }

    public void Dispose()
    {
        lock (_pendingSaveLock)
        {
            _disposed = true;
            _saveTimer?.Dispose();
            _saveTimer = null;
        }
        FlushPendingSave();
    }


    public const BindingFlags SettingsPropertiesFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    /// <summary>
    /// 添加应用设置叠层。
    /// </summary>
    /// <param name="guid">设置叠层 Guid。</param>
    /// <param name="name">设置属性名称。</param>
    /// <param name="value">要设置的值。</param>
    /// <returns>是否成功。失败会抛出。</returns>
    public bool AddSettingsOverlay(Guid guid, string name, object? value)
    {
        var key = guid.ToString();
        var isPropertyOverlayNotNull = Settings.SettingsOverlays.TryGetValue(name, out var propertyOverlay);
        if (isPropertyOverlayNotNull)
            propertyOverlay!.Remove(key);

        var info = typeof(Settings).GetProperty(name, SettingsPropertiesFlags);
        if (info == null)
        {
            throw new KeyNotFoundException($"应用设置中找不到属性“{name}”。");
            return false;
        }

        if (!isPropertyOverlayNotNull || propertyOverlay!.Contains(key))
        {
            propertyOverlay = [];
            var sourceValue = info.GetValue(Settings);
            if (value == sourceValue)
                return true;
            propertyOverlay["@"] = sourceValue;
        }

        info.SetValue(Settings, value);
        propertyOverlay[key] = value;
        Settings.SettingsOverlays[name] = propertyOverlay;
        return true;
    }

    // PropertyInfo 缓存。
    private static readonly ConcurrentDictionary<string, PropertyInfo?> _propertyCache = new();

    public static PropertyInfo? GetPropertyInfoByName(string name) =>
        string.IsNullOrEmpty(name) ? null :
            _propertyCache.GetOrAdd(name,
                key => typeof(Settings).GetProperty(key, SettingsPropertiesFlags));

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

        var last = propertyOverlay[length - 1]; // propertyOverlay 至少存在一项。
        if (last is JsonElement json)
            last = json.Deserialize(info.PropertyType);
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

        ScheduleSave(propertyName);
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