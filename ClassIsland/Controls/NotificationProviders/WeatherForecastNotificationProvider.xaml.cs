using ClassIsland.Models.Weather;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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