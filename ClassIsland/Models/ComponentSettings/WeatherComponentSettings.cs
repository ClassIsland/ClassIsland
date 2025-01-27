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

    private int _alertsTitleShowMode = 1;
    public int AlertsTitleShowMode
    {
        get => _alertsTitleShowMode;
        set
        {
            if (value == _alertsTitleShowMode) return;
            _alertsTitleShowMode = value;
            OnPropertyChanged();
        }
    }
}