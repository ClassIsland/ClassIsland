using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using ClassIsland.Core.Models.Weather;

namespace ClassIsland.Controls.NotificationProviders;

/// <summary>
/// WeatherForecastNotificationProvider.xaml 的交互逻辑
/// </summary>
public partial class WeatherForecastNotificationProvider : UserControl, INotifyPropertyChanged
{
    private bool _isOverlay = false;
    private WeatherInfo _info = new();

    public bool IsOverlay
    {
        get => _isOverlay;
        set
        {
            if (value == _isOverlay) return;
            _isOverlay = value;
            OnPropertyChanged();
        }
    }

    public WeatherInfo Info
    {
        get => _info;
        set
        {
            if (Equals(value, _info)) return;
            _info = value;
            OnPropertyChanged();
        }
    }

    public WeatherForecastNotificationProvider(bool isOverlay, WeatherInfo info)
    {
        IsOverlay = isOverlay;
        Info = info;
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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