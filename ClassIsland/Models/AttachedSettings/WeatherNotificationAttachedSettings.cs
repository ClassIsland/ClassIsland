using ClassIsland.Shared.Abstraction.Models;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Interfaces;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.AttachedSettings;

public class WeatherNotificationAttachedSettings : ObservableRecipient, IAttachedSettings, IWeatherNotificationSettingsBase
{
    private NotificationModes _alertShowMode = NotificationModes.Default;
    private NotificationModes _forecastShowMode = NotificationModes.Default;
    private bool _isAttachSettingsEnabled = true;

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

    public bool IsAttachSettingsEnabled
    {
        get => _isAttachSettingsEnabled;
        set
        {
            if (value == _isAttachSettingsEnabled) return;
            _isAttachSettingsEnabled = value;
            OnPropertyChanged();
        }
    }
}