using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

using ClassIsland.Models;
using ClassIsland.Services.Management;

using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class SettingsService(ILogger<SettingsService> logger, ManagementService managementService) : INotifyPropertyChanged
{
    private Settings _settings = new();

    public Settings Settings
    {
        get => _settings;
        set => SetField(ref _settings, value);
    }

    private ILogger<SettingsService> Logger { get; } = logger;

    private ManagementService ManagementService { get; } = managementService;

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
                Logger.LogInformation("配置文件不存在，跳过加载。");
            }
            else
            {
                Logger.LogInformation("加载配置文件。");
                var json = await File.ReadAllTextAsync("./Settings.json");
                var r = JsonSerializer.Deserialize<Settings>(json);
                if (r != null)
                {
                    Settings = r;
                    Settings.PropertyChanged += (sender, args) => SaveSettings();
                }
            }

            // 当还没有初始化应用且启用集控时，从集控拉取设置。
            if (ManagementService.IsManagementEnabled && !Settings.IsWelcomeWindowShowed)
            {
                await LoadManagementSettingsAsync();
            }
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "配置文件加载失败。");
            // ignored
        }
    }

    public void SaveSettings()
    {
        Logger.LogInformation("写入配置文件。");
        File.WriteAllText("./Settings.json", JsonSerializer.Serialize<Settings>(Settings));
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