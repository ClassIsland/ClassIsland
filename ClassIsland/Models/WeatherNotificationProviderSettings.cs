using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class WeatherNotificationProviderSettings : ObservableRecipient
{
    private List<string> _showedList = new();

    public List<string> ShowedList
    {
        get => _showedList;
        set
        {
            if (Equals(value, _showedList)) return;
            _showedList = value;
            OnPropertyChanged();
        }
    }
}