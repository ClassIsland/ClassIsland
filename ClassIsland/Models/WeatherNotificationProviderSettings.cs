using System.Collections.Generic;
using ClassIsland.Enums;
using ClassIsland.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class WeatherNotificationProviderSettings : ObservableRecipient, IWeatherNotificationSettingsBase
{
    private NotificationModes _alertShowMode;
    private NotificationModes _forecastShowMode;
    private bool _isAlertEnabled = true;
    private bool _isForecastEnabled = true;

    public NotificationModes AlertShowMode
    {
        get => _alertShowMode;
        set
        {
            if (value == _alertShowMode) return;
            _alertShowMode = value;
            OnPropertyChanged();
        }
    }

    public NotificationModes ForecastShowMode
    {
        get => _forecastShowMode;
        set
        {
            if (value == _forecastShowMode) return;
            _forecastShowMode = value;
            OnPropertyChanged();
        }
    }

    public bool IsAlertEnabled
    {
        get => _isAlertEnabled;
        set
        {
            if (value == _isAlertEnabled) return;
            _isAlertEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsForecastEnabled
    {
        get => _isForecastEnabled;
        set
        {
            if (value == _isForecastEnabled) return;
            _isForecastEnabled = value;
            OnPropertyChanged();
        }
    }
}