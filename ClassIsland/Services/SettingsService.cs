using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class SettingsService : IHostedService, INotifyPropertyChanged
{
    private Settings _settings = new();

    public Settings Settings
    {
        get => _settings;
        set => SetField(ref _settings, value);
    }

    private ILogger<SettingsService> Logger { get; }

    public SettingsService(IHostApplicationLifetime appLifetime, ILogger<SettingsService> logger)
    {
        Logger = logger;
        LoadSettings();
        appLifetime.ApplicationStopping.Register(SaveSettings);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void LoadSettings()
    {
        if (!File.Exists("./Settings.json"))
        {
            Logger.LogInformation("配置文件不存在，跳过加载。");
            return;
        }

        try
        {
            Logger.LogInformation("加载配置文件。");
            var json = File.ReadAllText("./Settings.json");
            var r = JsonSerializer.Deserialize<Settings>(json);
            if (r != null)
            {
                Settings = r;
                Settings.PropertyChanged += (sender, args) => SaveSettings();
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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LoadSettings();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        SaveSettings();
    }
}