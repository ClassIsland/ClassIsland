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
    private bool _showRainTime = true;

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

    public bool ShowRainTime
    {
        get => _showRainTime;
        set
        {
            if (value == _showRainTime) return;
            _showRainTime = value;
            OnPropertyChanged();
        }
    }
}