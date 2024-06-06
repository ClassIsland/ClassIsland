using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Models.Weather;
using ClassIsland.Services;
using ClassIsland.ViewModels.SettingsPages;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// WeatherSettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("weather", "天气", PackIconKind.CloudOutline, PackIconKind.Cloud, SettingsPageCategory.Internal)]
public partial class WeatherSettingsPage : SettingsPageBase
{
    public WeatherSettingsViewModel ViewModel { get; } = new();

    public SettingsService SettingsService { get; }

    public IWeatherService WeatherService { get; }
    public WeatherSettingsPage(SettingsService settingsService, IWeatherService weatherService)
    {
        InitializeComponent();
        DataContext = this;
        WeatherService = weatherService;
        SettingsService = settingsService;
        ViewModel.CitySearchResults = WeatherService.GetCitiesByName("");
    }

    private async void ButtonRefreshWeather_OnClick(object sender, RoutedEventArgs e)
    {
        await WeatherService.QueryWeatherAsync();
    }

    private void ButtonEditCurrentCity_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("CitySearcher");
    }

    private void TextBoxSearchCity_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.CitySearchResults = WeatherService.GetCitiesByName(((TextBox)sender).Text);
    }

    private async void SelectorCity_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var listbox = (ListBox)sender;
        var city = (City?)listbox.SelectedItem;
        if (city == null)
        {
            e.Handled = true;
            //Settings.CityName = "";
            return;
        }
        SettingsService.Settings.CityName = city.Name;
        await WeatherService.QueryWeatherAsync();
    }
}