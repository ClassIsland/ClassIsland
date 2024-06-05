using ClassIsland.Core.Models.Weather;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace ClassIsland.ViewModels.SettingsPages;

public class WeatherSettingsViewModel : ObservableRecipient
{
    private List<City> _citySearchResults = new();

    public List<City> CitySearchResults
    {
        get => _citySearchResults;
        set
        {
            if (Equals(value, _citySearchResults)) return;
            _citySearchResults = value;
            OnPropertyChanged();
        }
    }
}