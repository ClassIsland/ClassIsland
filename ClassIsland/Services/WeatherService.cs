using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Templates;
using Avalonia.Platform;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Ruleset;
using ClassIsland.Core.Models.Weather;
using ClassIsland.Helpers;
using ClassIsland.Models;
using ClassIsland.Models.Rules;
using ClassIsland.Platforms.Abstraction.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class WeatherService : ObservableRecipient, IHostedService, IWeatherService
{
    private IDataTemplate? _selectedWeatherIconTemplate;
    private SettingsService SettingsService { get; }

    private Settings Settings => SettingsService.Settings;
    private string Schema => Settings.NoTLSWeatherRequests ? "http" : "https";
    public List<XiaomiWeatherStatusCodeItem> WeatherStatusList { get; set; } = new();

    private ILogger<WeatherService> Logger { get; }
    public IRulesetService RulesetService { get; }
    public ILocationService LocationService { get; }

    private DispatcherTimer UpdateTimer { get; } = new()
    {
        Interval = TimeSpan.FromMinutes(5)
    };

    public bool IsWeatherRefreshed { get; set; } = false;

    public bool IsPosUpdated { get; set; } = false;

    public WeatherService(SettingsService settingsService, ILogger<WeatherService> logger, IRulesetService rulesetService, ILocationService locationService)
    {
        Logger = logger;
        RulesetService = rulesetService;
        LocationService = locationService;
        SettingsService = settingsService;
        SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
        LoadData();
        LoadWeatherIconTemplate();
        RulesetService.RegisterRuleHandler("classisland.weather.currentWeather", CurrentWeatherRuleHandler);
        RulesetService.RegisterRuleHandler("classisland.weather.tomorrowWeather", TomorrowWeatherRuleHandler);
        RulesetService.RegisterRuleHandler("classisland.weather.hasWeatherAlert", HasAlertRuleHandler);
        RulesetService.RegisterRuleHandler("classisland.weather.rainTime", RainTimeRuleHandler);
        RulesetService.RegisterRuleHandler("classisland.weather.sunRiseSet", SunRiseSetRuleHandler);
        UpdateTimer.Tick += UpdateTimerOnTick;
        UpdateTimer.Start();
        _ = QueryWeatherAsync();
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(Settings.WeatherIconId))
        {
            return;
        }

        LoadWeatherIconTemplate();
    }

    private void LoadWeatherIconTemplate()
    {
        SelectedWeatherIconTemplate = IWeatherService.RegisteredTemplates.FirstOrDefault(x => x.Id == Settings.WeatherIconId)?.Template;
        if (SelectedWeatherIconTemplate == null)
        {
            Logger.LogWarning("未找到 ID 为 {} 的天气图标模板", Settings.WeatherIconId);
        }
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

    private bool SunRiseSetRuleHandler(object? o)
    {
        if (o is not SunRiseSetRuleSettings settings)
            return false;
        if (!IsWeatherRefreshed)
            return false;

        var now = DateTimeOffset.Now;
        if (!TryGetSunTimes(now, out var sunrise, out var sunset))
            return false;

        return settings.IsSunset
            ? (now >= sunset || now < sunrise)
            : (now >= sunrise && now < sunset);
    }

    private bool TryGetSunTimes(DateTimeOffset now, out DateTimeOffset sunrise, out DateTimeOffset sunset)
    {
        sunrise = default;
        sunset = default;
        var list = Settings.LastWeatherInfo.ForecastDaily.SunRiseSet.Value;
        foreach (var item in list)
        {
            if (!DateTimeOffset.TryParse(item.From, CultureInfo.InvariantCulture, DateTimeStyles.None, out var sr))
                continue;
            if (!DateTimeOffset.TryParse(item.To, CultureInfo.InvariantCulture, DateTimeStyles.None, out var ss))
                continue;
            if (sr.Date == now.Date || ss.Date == now.Date)
            {
                sunrise = sr;
                sunset = ss;
                return true;
            }
        }
        return false;
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
               (settings.IsFuzzyMatch
                   ? IsWeatherCodeMatched(Settings.LastWeatherInfo.Current.Weather, settings.WeatherId.ToString())
                   : settings.WeatherId.ToString() == Settings.LastWeatherInfo.Current.Weather);
    }

    private bool TomorrowWeatherRuleHandler(object? o)
    {
        if (o is not CurrentWeatherRuleSettings settings)
        {
            return false;
        }

        if (!IsWeatherRefreshed || Settings.LastWeatherInfo.ForecastDaily.Weather.Value == null || Settings.LastWeatherInfo.ForecastDaily.Weather.Value.Count <= 1)
        {
            return false;
        }

        var tomorrowWeather = Settings.LastWeatherInfo.ForecastDaily.Weather.Value[1];
        var targetWeather = settings.WeatherId.ToString();

        if (settings.IsFuzzyMatch)
        {
            return IsWeatherCodeMatched(tomorrowWeather.From, targetWeather) ||
                   IsWeatherCodeMatched(tomorrowWeather.To, targetWeather);
        }
        else
        {
            return tomorrowWeather.From == targetWeather || tomorrowWeather.To == targetWeather;
        }
    }

    private static bool IsWeatherCodeMatched(string actualWeatherCode, string targetWeatherCode)
    {
        if (actualWeatherCode == targetWeatherCode)
        {
            return true;
        }

        var actualCodes = ExpandWeatherCode(actualWeatherCode);
        var targetCodes = ExpandWeatherCode(targetWeatherCode);

        return actualCodes.Overlaps(targetCodes);
    }

    private static HashSet<string> ExpandWeatherCode(string weatherCode)
    {
        return weatherCode switch
        {
            "21" => ["7", "8", "21"],
            "22" => ["8", "9", "22"],
            "23" => ["9", "10", "23"],
            "24" => ["10", "11", "24"],
            "25" => ["11", "12", "25"],
            "26" => ["14", "15", "26"],
            "27" => ["15", "16", "27"],
            "28" => ["16", "17", "28"],
            "301" => ["3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "19", "21", "22", "23", "24", "25", "301"],
            "302" => ["6", "13", "14", "15", "16", "17", "26", "27", "28", "34", "302"],
            _ => [weatherCode]
        };
    }

    private async void UpdateTimerOnTick(object? sender, EventArgs e)
    {
        await QueryWeatherAsync();
    }

    private async void LoadData()
    {
        var w = AssetLoader.Open(new Uri("avares://ClassIsland/Assets/XiaomiWeather/xiaomi_weather_status.json",
            UriKind.Absolute));
        var codes = await JsonSerializer.DeserializeAsync<XiaomiWeatherStatusCodes>(w);
        WeatherStatusList = codes!.WeatherInfo;
    }

    public async Task QueryWeatherAsync()
    {
        if (!IsPosUpdated && Settings.AutoRefreshWeatherLocation)
        {
            IsPosUpdated = true;
            try
            {
                var pos = await LocationService.GetLocationAsync();
                SettingsService.Settings.WeatherLongitude = Math.Round(pos.Longitude, 4);
                SettingsService.Settings.WeatherLatitude = Math.Round(pos.Latitude, 4);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "无法获取当前位置");
            }
        }

        var cityLatitude = string.Empty;
        var cityLongitude = string.Empty;

        // 获取城市信息
        try
        {
            using var http = new HttpClient();
            var uri = Settings.WeatherLocationSource switch
            {
                0 => $"{Schema}://weatherapi.market.xiaomi.com/wtr-v3/location/city/info?locationKey={Settings.CityId}&locale=zh_cn",
                1 => $"{Schema}://weatherapi.market.xiaomi.com/wtr-v3/location/city/geo?longitude={Settings.WeatherLongitude}&latitude={Settings.WeatherLatitude}&locale=zh_cn",
                _ => throw new ArgumentOutOfRangeException()
            };
            Logger.LogInformation("获取城市信息： {}", uri);
            var cityInfoList = await WebRequestHelper.Default.GetJson<List<CityInfo>>(new Uri(uri));
            // 取第一个城市信息
            var cityInfo = cityInfoList.FirstOrDefault();
            if (cityInfo != null && (Settings.WeatherLocationSource != 0 || cityInfo.LocationKey == Settings.CityId)
                && !string.IsNullOrWhiteSpace(cityInfo.LocationKey))
            {
                cityLatitude = cityInfo.Latitude;
                cityLongitude = cityInfo.Longitude;
                if (Settings.WeatherLocationSource == 1)
                {
                    cityLatitude = Settings.WeatherLatitude.ToString(CultureInfo.InvariantCulture);
                    cityLongitude = Settings.WeatherLongitude.ToString(CultureInfo.InvariantCulture);
                    Settings.CityId = cityInfo.LocationKey;
                    Settings.CityName = $"{cityInfo.Name} ({cityInfo.Affiliation})";
                }
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
                $"{Schema}://weatherapi.market.xiaomi.com/wtr-v3/weather/all?latitude={cityLatitude}&longitude={cityLongitude}&locationKey={Uri.EscapeDataString(Settings.CityId)}&days=15&appKey=weather20151024&sign=zUFJoAR2ZVrDy1vF3D07&isGlobal=false&locale=zh_cn";
            Logger.LogInformation("获取天气信息： {}", uri);
            var info = await WebRequestHelper.Default.GetJson<WeatherInfo>(new Uri(uri));

            // 排除天气预警
            var validExclusions = Settings.ExcludedWeatherAlerts
                .Where(x => !string.IsNullOrWhiteSpace(x));

            info.Alerts.RemoveAll(alert =>
                validExclusions
                    .Any(exclusion =>
                        alert.Title.Contains(exclusion) || alert.Detail.Contains(exclusion)
                )
            );

            // 去重天气预警
            var latest = info.Alerts
                .GroupBy(a => a.Type)
                .ToDictionary(g => g.Key, g => g.MaxBy(a => a.PubTime)!);
            foreach (var a in info.Alerts)
            {
                if (latest[a.Type].AlertId != a.AlertId)
                    Logger.LogInformation("已丢弃旧预警：（{}，{}）{}", a.Title, a.PubTime, a.Detail);
            }
            info.Alerts = latest.Values.ToList();

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
        var uri = new Uri($"{Schema}://weatherapi.market.xiaomi.com/wtr-v3/location/city/hots?locale=zh_cn");
        var logText = "获取热门城市信息";

        if (name != string.Empty)
        {
            uri = new Uri(
                $"{Schema}://weatherapi.market.xiaomi.com/wtr-v3/location/city/search?name={Uri.EscapeDataString(name)}&locale=zh_cn");
            logText = logText.Replace("热门", "");
        }

        try
        {
            Logger.LogInformation("{}： {}", logText, uri);

            var cityInfoList = await WebRequestHelper.Default.GetJson<List<CityInfo>>(uri);

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

    public IDataTemplate? SelectedWeatherIconTemplate
    {
        get => _selectedWeatherIconTemplate;
        private set => SetProperty(ref _selectedWeatherIconTemplate, value);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}
