using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Models.Weather;
using ClassIsland.Services;
using ClassIsland.ViewModels.SettingsPages;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;

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
    public ILocationService LocationService { get; }
    public ILogger<WeatherSettingsPage> Logger { get; }

    // [搜索城市或地区] TextBox的全局变量 用于防抖
    private TextBox GlobalTextBoxSearchCity { get; set; } = null!;

    // [搜索城市或地区] 防抖定时器
    private DispatcherTimer SearchDebounceTimer { get; set; } = null!;

    public WeatherSettingsPage(SettingsService settingsService, IWeatherService weatherService, ILocationService locationService, ILogger<WeatherSettingsPage> logger)
    {
        InitializeComponent();
        DataContext = this;
        WeatherService = weatherService;
        LocationService = locationService;
        Logger = logger;
        SettingsService = settingsService;
        // [搜索城市或地区] 初始化防抖定时器
        Loaded += WeatherSettingsPage_Loaded;
    }

    private async void ButtonRefreshWeather_OnClick(object sender, RoutedEventArgs e)
    {
        await WeatherService.QueryWeatherAsync();
    }

    private void ButtonEditCurrentCity_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("CitySearcher");
    }

    /// <summary>
    /// [搜索城市或地区] 防抖定时器初始化
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void WeatherSettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        // 初始化防抖定时器，设置间隔时间为50毫秒
        SearchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        SearchDebounceTimer.Tick += SearchDebounceTimer_Tick;
        SearchDebounceTimer.Stop();
        
        ViewModel.CitySearchResults = await WeatherService.GetCitiesByName(string.Empty);
    }

    /// <summary>
    /// [搜索城市或地区] TextBox的文本改变事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void TextBoxSearchCity_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        // 为全局变量赋值
        GlobalTextBoxSearchCity = (TextBox)sender;

        // 重置定时器
        SearchDebounceTimer.Stop();
        SearchDebounceTimer.Start();
    }

    /// <summary>
    /// [搜索城市或地区] 防抖定时器触发事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void SearchDebounceTimer_Tick(object? sender, EventArgs e)
    {
        // 停止定时器
        SearchDebounceTimer.Stop();

        // 更新搜索结果
        ViewModel.IsSearchingWeather = true;
        var searchText = GlobalTextBoxSearchCity.Text;
        ViewModel.CitySearchResults =
            await WeatherService.GetCitiesByName(searchText);
        ViewModel.IsSearchingWeather = false;
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

    private async void ButtonGetCurrentPos_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var pos = await LocationService.GetLocationAsync();
            SettingsService.Settings.WeatherLongitude = Math.Round(pos.Longitude, 4); 
            SettingsService.Settings.WeatherLatitude = Math.Round(pos.Latitude, 4);
            await WeatherService.QueryWeatherAsync();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "无法获取当前位置");
        }
    }

    private void ButtonShowPos_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.HideLocationPos = false;
    }
}