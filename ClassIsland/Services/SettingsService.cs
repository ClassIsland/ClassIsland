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

namespace ClassIsland.Services;

public class SettingsService : IHostedService, INotifyPropertyChanged
{
    private Settings _settings = new();

    public Settings Settings
    {
        get => _settings;
        set => SetField(ref _settings, value);
    }

    public SettingsService(IHostApplicationLifetime appLifetime)
    {
        LoadSettings();
        appLifetime.ApplicationStopping.Register(SaveSettings);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void LoadSettings()
    {
        if (!File.Exists("./Settings.json"))
        {
            return;
        }

        try
        {
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
            // ignored
        }
    }

    public void SaveSettings()
    {
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