﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Ruleset;
using ClassIsland.Core.Models.Weather;
using ClassIsland.Helpers;
using ClassIsland.Models;
using ClassIsland.Models.Rules;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class WeatherService : IHostedService, IWeatherService
{
    private SettingsService SettingsService { get; }

    private Settings Settings => SettingsService.Settings;

    public List<XiaomiWeatherStatusCodeItem> WeatherStatusList { get; set; } = new();

    private ILogger<WeatherService> Logger { get; }
    public IRulesetService RulesetService { get; }

    private DispatcherTimer UpdateTimer { get; } = new()
    {
        Interval = TimeSpan.FromMinutes(5)
    };

    public bool IsWeatherRefreshed { get; set; } = false;

    public WeatherService(SettingsService settingsService, ILogger<WeatherService> logger, IRulesetService rulesetService)
    {
        Logger = logger;
        RulesetService = rulesetService;
        SettingsService = settingsService;
        LoadData();
        RulesetService.RegisterRuleHandler("classisland.weather.currentWeather", CurrentWeatherRuleHandler);
        RulesetService.RegisterRuleHandler("classisland.weather.hasWeatherAlert", HasAlertRuleHandler);
        RulesetService.RegisterRuleHandler("classisland.weather.rainTime", RainTimeRuleHandler);
        UpdateTimer.Tick += UpdateTimerOnTick;
        UpdateTimer.Start();
        _ = QueryWeatherAsync();
    }

    private bool RainTimeRuleHandler(object? o)
    {
        if (o is not RainTimeRuleSettings settings)
        {
            return false;
        }

        var baseTime = (settings.IsRemainingTime ? -1.0 : 1.0) * Settings.LastWeatherInfo.Minutely.Precipitation.RainRemainingMinutes;
        return baseTime > 0 && baseTime <= settings.RainTimeMinutes;
        
    }

    private bool HasAlertRuleHandler(object? o)
    {
        if (o is not StringMatchingSettings settings)
        {
            return false;
        }

        return IsWeatherRefreshed &&
               Settings.LastWeatherInfo.Alerts.Exists(x => settings.IsMatching(x.Title));
    }

    private bool CurrentWeatherRuleHandler(object? o)
    {
        if (o is not CurrentWeatherRuleSettings settings)
        {
            return false;
        }

        return IsWeatherRefreshed &&
               settings.WeatherId.ToString() == Settings.LastWeatherInfo.Current.Weather;
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
            
            // Apply TitleFix logic to each alert
            foreach (var alert in info.Alerts)
            {
                var publishIndex = alert.Title.IndexOf("发布", StringComparison.Ordinal);
                if (publishIndex > 0)
                {
                    alert.Title = alert.Title.Substring(publishIndex + 2);
                }

                var detailEndIndex = alert.Detail.IndexOf("气象", StringComparison.Ordinal);
                var detailPart = detailEndIndex > 0 ? alert.Detail.Substring(0, detailEndIndex) : alert.Detail;
                alert.Title = $"{detailPart}发布{alert.Title}";
            }

            Settings.LastWeatherInfo = info;
            IsWeatherRefreshed = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取天气信息失败。");
        }

        RulesetService.NotifyStatusChanged();
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

