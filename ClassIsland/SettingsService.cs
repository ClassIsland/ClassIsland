using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Models;
using ClassIsland.Views;
using Microsoft.Extensions.Hosting;

namespace ClassIsland;

public class SettingsService : BackgroundService, INotifyPropertyChanged
{
    private Settings _settings = new();

    public Settings Settings
    {
        get => _settings;
        set => SetField(ref _settings, value);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => null;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void LoadSettings()
    {
        if (!File.Exists("./Settings.json"))
        {
            return;
        }
        var json = File.ReadAllText("./Settings.json");
        var r = JsonSerializer.Deserialize<Settings>(json);
        if (r != null)
        { 
            Settings = r;
            Settings.PropertyChanged += (sender, args) => SaveSettings();
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

    public async override Task StartAsync(CancellationToken cancellationToken)
    {
        LoadSettings();
    }

    public async override Task StopAsync(CancellationToken cancellationToken)
    {
        SaveSettings();
    }
}