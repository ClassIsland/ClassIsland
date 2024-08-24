using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.ComponentSettings;

public class WeatherComponentSettings : ObservableRecipient
{
    private bool _showAlerts = true;

    public bool ShowAlerts
    {
        get => _showAlerts;
        set
        {
            if (value == _showAlerts) return;
            _showAlerts = value;
            OnPropertyChanged();
        }
    }
}