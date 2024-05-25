using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

using ClassIsland.Models.Weather;

namespace ClassIsland.Controls.NotificationProviders;

/// <summary>
/// WeatherNotificationProviderControl.xaml 的交互逻辑
/// </summary>
public partial class WeatherNotificationProviderControl : UserControl, INotifyPropertyChanged
{
    private WeatherAlert _alert = new WeatherAlert();
    private bool _isOverlay;

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

    public WeatherAlert Alert
    {
        get => _alert;
        set
        {
            if (Equals(value, _alert)) return;
            _alert = value;
            OnPropertyChanged();
        }
    }

    public WeatherNotificationProviderControl(bool isOverlay, WeatherAlert alert)
    {
        InitializeComponent();
        IsOverlay = isOverlay;
        Alert = alert;
    }

    protected override void OnInitialized(EventArgs e)
    {
        
        base.OnInitialized(e);
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

    private void WeatherNotificationProviderControl_OnLoaded(object sender, RoutedEventArgs e)
    {
        App.GetService<MainWindow>().GetCurrentDpi(out var dpi, out _);
        var da = new DoubleAnimation()
        {
            From = -Description.ActualWidth,
            To = RootCanvas.ActualWidth,
            Duration = new Duration(TimeSpan.FromSeconds(20)),

        };
        var storyboard = new Storyboard()
        {
        };
        Storyboard.SetTarget(da, Description);
        Storyboard.SetTargetProperty(da, new PropertyPath(Canvas.RightProperty));
        storyboard.Children.Add(da);
        storyboard.RepeatBehavior = RepeatBehavior.Forever;
        storyboard.Begin();
        //storyboard.Begin();
    }
}