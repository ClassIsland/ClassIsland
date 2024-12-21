using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Weather;
using ClassIsland.Helpers;
using ClassIsland.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class WeatherService : IHostedService, IWeatherService
{
    private SettingsService SettingsService { get; }

    private Settings Settings => SettingsService.Settings;

    public List<XiaomiWeatherStatusCodeItem> WeatherStatusList { get; set; } = new();

    private ILogger<WeatherService> Logger { get; }

    private DispatcherTimer UpdateTimer { get; } = new()
    {
        Interval = TimeSpan.FromMinutes(5)
    };

    public bool IsWeatherRefreshed { get; set; } = false;

    public WeatherService(SettingsService settingsService, ILogger<WeatherService> logger)
    {
        Logger = logger;
        SettingsService = settingsService;
        LoadData();
        UpdateTimer.Tick += UpdateTimerOnTick;
        UpdateTimer.Start();
        _ = QueryWeatherAsync();
    }

    private async void UpdateTimerOnTick(object? sender, EventArgs e)
    {
        await QueryWeatherAsync();
    }

    private async void LoadData()
    {
        var w = Application.GetResourceStream(new Uri("/Assets/XiaomiWeather/xiaomi_weather_status.json",
            UriKind.Relative));
        if (w == null) return;
        var codes = await JsonSerializer.DeserializeAsync<XiaomiWeatherStatusCodes>(w.Stream);
        WeatherStatusList = codes!.WeatherInfo;
    }

    public async Task QueryWeatherAsync()
    {
        var cityLatitude = string.Empty;
        var cityLongitude = string.Empty;
        
        // 获取城市信息
        try
        {
            using var http = new HttpClient();
            var uri =
                $"https://weatherapi.market.xiaomi.com/wtr-v3/location/city/info?locationKey={Settings.CityId}&locale=zh_cn";
            Logger.LogInformation("获取城市信息： {}", uri);
            var cityInfoList = await WebRequestHelper.GetJson<List<CityInfo>>(new Uri(uri));
            // 取第一个城市信息
            var cityInfo = cityInfoList.FirstOrDefault();
            if (cityInfo != null && cityInfo.LocationKey == Settings.CityId)
            {
                cityLatitude = cityInfo.Latitude;
                cityLongitude = cityInfo.Longitude;       
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取城市信息失败。");
        }
        
        // 请求天气信息
        try
        {
            using var http = new HttpClient();
            var uri =
                $"https://weatherapi.market.xiaomi.com/wtr-v3/weather/all?latitude={cityLatitude}&longitude={cityLongitude}&locationKey={Uri.EscapeDataString(Settings.CityId)}&days=15&appKey=weather20151024&sign=zUFJoAR2ZVrDy1vF3D07&isGlobal=false&locale=zh_cn";
            Logger.LogInformation("获取天气信息： {}", uri);
            var info = await WebRequestHelper.GetJson<WeatherInfo>(new Uri(uri));
            info.Alerts.RemoveAll(i => Settings.ExcludedWeatherAlerts.FirstOrDefault(x =>
                (!string.IsNullOrWhiteSpace(x)) && i.Title.Contains(x)) != null);
            Settings.LastWeatherInfo = info;
            IsWeatherRefreshed = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取天气信息失败。");
        }
    }

    public string GetWeatherTextByCode(string code)
    {
        var c = (from i in WeatherStatusList
                where i.Code.ToString() == code
                select i.Weather)
            .ToList();
        return c.Count > 0 ? c[0] : "未知";
    }

    public async Task<List<City>> GetCitiesByName(string name)
    {
        var uri = new Uri("https://weatherapi.market.xiaomi.com/wtr-v3/location/city/hots?locale=zh_cn");
        var logText = "获取热门城市信息";

        if (name != string.Empty)
        {
            uri = new Uri(
                $"https://weatherapi.market.xiaomi.com/wtr-v3/location/city/search?name={Uri.EscapeDataString(name)}&locale=zh_cn");
            logText = logText.Replace("热门", "");
        }

        try
        {
            Logger.LogInformation("{}： {}", logText, uri);

            var cityInfoList = await WebRequestHelper.GetJson<List<CityInfo>>(uri);
            
            var cities = cityInfoList?.Select(cityInfo => new City
            {
                Name = $"{cityInfo.Name} ({cityInfo.Affiliation})",
                CityId = cityInfo.LocationKey
            }).ToList() ?? new List<City>();

            return cities;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{}失败。", logText);
            return [];
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}