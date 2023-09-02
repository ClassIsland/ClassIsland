using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class WeatherMiniInfoProviderSettings : ObservableRecipient
{
    private bool _isAlertEnabled = true;

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
}