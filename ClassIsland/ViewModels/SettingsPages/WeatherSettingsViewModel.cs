using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using ClassIsland.Core.Models.Weather;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Services;
using ClassIsland.Views.SettingPages;
using Microsoft.Extensions.Logging;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class WeatherSettingsViewModel : ObservableRecipient
{
    private readonly SettingsService _settingsService;
    private readonly ILocationService _locationService;
    private readonly IWeatherService _weatherService;
    private readonly ILogger<WeatherSettingsPage> _logger;
    private readonly DispatcherTimer _searchDebounceTimer;
    private string _pendingSearchText = string.Empty;

    public SettingsService SettingsService => _settingsService;
    public ILocationService LocationService => _locationService;
    public IWeatherService WeatherService => _weatherService;
    public ILogger<WeatherSettingsPage> Logger => _logger;

    [ObservableProperty] private List<City> _citySearchResults = new();
    [ObservableProperty] private bool _isSearchingWeather;
    [ObservableProperty] private bool _isRefreshingWeather;
    [ObservableProperty] private bool _isLocationUpdating;
    [ObservableProperty] private int _selectedLocationSource;
    [ObservableProperty] private bool _hideLocationPos = true;


    public WeatherSettingsViewModel(
        SettingsService settingsService,
        ILocationService locationService,
        IWeatherService weatherService,
        ILogger<WeatherSettingsPage> logger)
    {
        _settingsService = settingsService;
        _locationService = locationService;
        _weatherService = weatherService;
        _logger = logger;

        _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;

        SelectedLocationSource = settingsService.Settings.WeatherLocationSource;
        settingsService.Settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsService.Settings.WeatherLocationSource))
            {
                SelectedLocationSource = settingsService.Settings.WeatherLocationSource;
            }
        };
    }

    public async Task InitializeAsync()
    {
        CitySearchResults = await _weatherService.GetCitiesByName(string.Empty);
    }

    public void OnSearchTextChanged(string? searchText)
    {
        _pendingSearchText = searchText ?? string.Empty;
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    private async void SearchDebounceTimer_Tick(object? sender, EventArgs e)
    {
        _searchDebounceTimer.Stop();
        IsSearchingWeather = true;
        try
        {
            CitySearchResults = await _weatherService.GetCitiesByName(_pendingSearchText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search cities");
        }
        finally
        {
            IsSearchingWeather = false;
        }
    }

    [RelayCommand]
    private async Task RefreshWeatherAsync()
    {
        IsRefreshingWeather = true;
        try
        {
            await _weatherService.QueryWeatherAsync();
        }
        finally
        {
            IsRefreshingWeather = false;
        }
    }

    public async Task SelectCityAsync(City? city)
    {
        if (city == null) return;
        _settingsService.Settings.CityName = city.Name;
        await RefreshWeatherAsync();
    }

    [RelayCommand]
    public async Task GetCurrentPositionAsync()
    {
        IsLocationUpdating = true;
        try
        {
            var pos = await _locationService.GetLocationAsync();
            _settingsService.Settings.WeatherLongitude = Math.Round(pos.Longitude, 4);
            _settingsService.Settings.WeatherLatitude = Math.Round(pos.Latitude, 4);
            await RefreshWeatherAsync();
        }
        finally
        {
            IsLocationUpdating = false;
        }
    }

    public void ShowLocationCoordinates()
    {
        HideLocationPos = false;
    }

    partial void OnSelectedLocationSourceChanged(int value)
    {
        if (value == _settingsService.Settings.WeatherLocationSource) return;
        _settingsService.Settings.WeatherLocationSource = value;
    }
}
