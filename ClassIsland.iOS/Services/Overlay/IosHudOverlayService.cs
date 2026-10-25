using System.ComponentModel;
using System.Runtime.Versioning;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.iOS.Services.Platform;

namespace ClassIsland.iOS.Services.Overlay;

/// <summary>
/// TrollStore 发行版的系统级悬浮主界面服务。
/// 负责准备只读数据快照、拉起/终止 root HUD 子进程，并在配置变化时重启宿主。
/// </summary>
[SupportedOSPlatform("ios15.0")]
internal sealed class IosHudOverlayService : IMainWindowOverlayService
{
    private const string HudDataFolderName = "HudData";
    private static readonly TimeSpan RestartDebounce = TimeSpan.FromMilliseconds(1000);

    private bool _isEnabled;
    private bool _isApplyingSetting;
    private bool _isSubscribed;
    private Settings? _settings;
    private DispatcherTimer? _restartTimer;
    private DispatcherTimer? _livenessTimer;

    /// <inheritdoc />
    public bool IsSupported => true;

    /// <inheritdoc />
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetEnabled(value, persist: true);
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 在主 App 启动完成后恢复上次的悬浮主界面状态。
    /// </summary>
    public void ResumeIfEnabled()
    {
        if (IosHudMode.IsHudProcess || _isEnabled)
        {
            return;
        }

        if (TryGetSettings() is { IsIosHudOverlayEnabled: true })
        {
            SetEnabled(true, persist: false);
        }
    }

    /// <summary>
    /// 主 App 退出时终止悬浮主界面宿主，避免留下孤儿进程。
    /// </summary>
    public void Shutdown()
    {
        _restartTimer?.Stop();
        _restartTimer = null;
        _livenessTimer?.Stop();
        _livenessTimer = null;
        UnsubscribeConfigurationChanges();
        if (_isEnabled)
        {
            StopHud();
        }
    }

    private void SetEnabled(bool value, bool persist)
    {
        if (IosHudMode.IsHudProcess)
        {
            return;
        }

        IosStartupDiagnostics.Write($"悬浮主界面开关请求：{value}");

        if (value == _isEnabled)
        {
            if (persist)
            {
                PersistSetting(value);
            }

            return;
        }

        if (value)
        {
            if (!TryStartHud())
            {
                // 启动失败时通知 UI 回到未启用状态，避免开关显示为已开启但宿主并未运行。
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
                return;
            }

            _isEnabled = true;
            SubscribeConfigurationChanges();
            ScheduleHudLivenessCheck();
        }
        else
        {
            _isEnabled = false;
            UnsubscribeConfigurationChanges();
            _livenessTimer?.Stop();
            _livenessTimer = null;
            StopHud();
        }

        if (persist)
        {
            PersistSetting(value);
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
    }

    private bool TryStartHud()
    {
        try
        {
            if (IosHudNativeBridge.IsRunning() == 1)
            {
                IosHudNativeBridge.Stop();
            }

            var dataDirectory = PrepareSnapshot();
            IosStartupDiagnostics.Write($"悬浮主界面数据快照：{dataDirectory}");
            var result = IosHudNativeBridge.Start(1, dataDirectory);
            if (result == 0)
            {
                IosStartupDiagnostics.Write($"悬浮主界面宿主已启动：pid={IosHudNativeBridge.GetPid()}");
                return true;
            }

            IosStartupDiagnostics.Write(
                $"无法启动悬浮主界面宿主：result={result}，{IosHudNativeBridge.GetLastError()}");
            return false;
        }
        catch (Exception exception)
        {
            IosStartupDiagnostics.Write("启动悬浮主界面宿主时发生异常", exception);
            return false;
        }
    }

    /// <summary>
    /// spawn 后短延迟探活一次；HUD 子进程若在启动早期退出，恢复开关状态并记录原因。
    /// </summary>
    private void ScheduleHudLivenessCheck()
    {
        _livenessTimer?.Stop();
        _livenessTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _livenessTimer.Tick += (_, _) =>
        {
            _livenessTimer?.Stop();
            _livenessTimer = null;
            if (!_isEnabled)
            {
                return;
            }

            if (IosHudNativeBridge.IsRunning() == 1)
            {
                return;
            }

            IosStartupDiagnostics.Write("HUD 子进程在启动后退出；已自动关闭悬浮主界面开关");
            _isEnabled = false;
            UnsubscribeConfigurationChanges();
            PersistSetting(false);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
        };
        _livenessTimer.Start();
    }

    private static void StopHud()
    {
        try
        {
            if (IosHudNativeBridge.IsRunning() == 1 || IosHudNativeBridge.GetPid() > 0)
            {
                var result = IosHudNativeBridge.Stop();
                if (result < 0)
                {
                    IosStartupDiagnostics.Write(
                        $"终止悬浮主界面宿主失败：result={result}，{IosHudNativeBridge.GetLastError()}");
                }
            }
        }
        catch (Exception exception)
        {
            IosStartupDiagnostics.Write("终止悬浮主界面宿主时发生异常", exception);
        }
    }

    /// <summary>
    /// 为 HUD 子进程生成只读数据快照，避免两个进程并发写同一份配置。
    /// 插件、日志、缓存等不在快照内，因此 HUD 进程不会加载插件。
    /// 每次使用新的目录，避免主 App 删除 root HUD 进程创建的文件时触发权限错误。
    /// </summary>
    private static string PrepareSnapshot()
    {
        var sourceRoot = CommonDirectories.AppRootFolderPath;
        var snapshotRoot = Path.Combine(CommonDirectories.AppSharedDocumentsFolderPath, HudDataFolderName);
        var targetRoot = Path.Combine(snapshotRoot, $"Snapshot-{Guid.NewGuid():N}");
        Directory.CreateDirectory(targetRoot);

        CopyFileIfExists(Path.Combine(sourceRoot, "Settings.json"), Path.Combine(targetRoot, "Settings.json"));
        CopyFileIfExists(
            Path.Combine(sourceRoot, "ManagementPreset.json"),
            Path.Combine(targetRoot, "ManagementPreset.json"));
        CopyDirectoryIfExists(Path.Combine(sourceRoot, "Profiles"), Path.Combine(targetRoot, "Profiles"));
        CopyDirectoryIfExists(Path.Combine(sourceRoot, "Config"), Path.Combine(targetRoot, "Config"));

        return targetRoot;
    }

    private static void CopyFileIfExists(string source, string target)
    {
        if (!File.Exists(source))
        {
            return;
        }

        File.Copy(source, target, overwrite: true);
    }

    private static void CopyDirectoryIfExists(string source, string target)
    {
        if (!Directory.Exists(source))
        {
            return;
        }

        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, directory);
            if (IsExcludedSnapshotDirectory(relative))
            {
                continue;
            }

            Directory.CreateDirectory(Path.Combine(target, relative));
        }

        Directory.CreateDirectory(target);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            if (IsExcludedSnapshotDirectory(relative))
            {
                continue;
            }

            var destination = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }

    private static bool IsExcludedSnapshotDirectory(string relativePath)
    {
        var segments = relativePath.Split(Path.DirectorySeparatorChar);
        return segments.Any(segment =>
            string.Equals(segment, "ExternalAudios", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(segment, "Cache", StringComparison.OrdinalIgnoreCase));
    }

    private void SubscribeConfigurationChanges()
    {
        if (_isSubscribed)
        {
            return;
        }

        if (TryGetSettings() is { } settings)
        {
            settings.PropertyChanged += SettingsOnPropertyChanged;
        }

        if (IAppHost.TryGetService<IProfileService>() is INotifyPropertyChanged profileChanges)
        {
            profileChanges.PropertyChanged += ProfileOnPropertyChanged;
        }

        _isSubscribed = true;
    }

    private void UnsubscribeConfigurationChanges()
    {
        if (!_isSubscribed)
        {
            return;
        }

        if (_settings != null)
        {
            _settings.PropertyChanged -= SettingsOnPropertyChanged;
        }

        if (IAppHost.TryGetService<IProfileService>() is INotifyPropertyChanged profileChanges)
        {
            profileChanges.PropertyChanged -= ProfileOnPropertyChanged;
        }

        _isSubscribed = false;
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isApplyingSetting || e.PropertyName == nameof(Settings.IsIosHudOverlayEnabled))
        {
            return;
        }

        ScheduleRestart();
    }

    private void ProfileOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => ScheduleRestart();

    private void ScheduleRestart()
    {
        if (!_isEnabled)
        {
            return;
        }

        if (_restartTimer == null)
        {
            _restartTimer = new DispatcherTimer { Interval = RestartDebounce };
            _restartTimer.Tick += RestartTimerOnTick;
        }

        _restartTimer.Stop();
        _restartTimer.Start();
    }

    private void RestartTimerOnTick(object? sender, EventArgs e)
    {
        _restartTimer?.Stop();
        if (!_isEnabled)
        {
            return;
        }

        IosStartupDiagnostics.Write("配置已变化，正在重启悬浮主界面宿主");
        StopHud();
        if (!TryStartHud())
        {
            IosStartupDiagnostics.Write("配置变化后重启悬浮主界面宿主失败");
        }
    }

    private void PersistSetting(bool value)
    {
        if (TryGetSettings() is not { } settings)
        {
            return;
        }

        _isApplyingSetting = true;
        try
        {
            settings.IsIosHudOverlayEnabled = value;
        }
        finally
        {
            _isApplyingSetting = false;
        }
    }

    private Settings? TryGetSettings()
    {
        if (_settings != null)
        {
            return _settings;
        }

        if (IAppHost.TryGetService<SettingsService>() is { } settingsService)
        {
            _settings = settingsService.Settings;
        }

        return _settings;
    }
}
