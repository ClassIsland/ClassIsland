using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ClassIsland.Models;
using ClassIsland.Models.Weather;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services;

public class WeatherService : IHostedService
{
    public const string CitiesDatabasePath = "./Temp/xiaomi_weather.db";

    private SettingsService SettingsService { get; }

    private Settings Settings => SettingsService.Settings;

    public SqliteConnection CitiesDatabaseConnection { get; } = new($"Data Source={CitiesDatabasePath}");

    public List<XiaomiWeatherStatusCodeItem> WeatherStatusList { get; set; } = new();

    public WeatherService(SettingsService settingsService, FileFolderService fileFolderService, IHostApplicationLifetime hostApplicationLifetime)
    {
        SettingsService = settingsService;
        hostApplicationLifetime.ApplicationStopping.Register(AppStopping);
        LoadData();
    }

    private async void AppStopping()
    {
        await CitiesDatabaseConnection.CloseAsync();
    }

    private async void LoadData()
    {
        var s = Application.GetResourceStream(new Uri("/Assets/XiaomiWeather/xiaomi_weather.db", UriKind.Relative));
        if (s != null)
        {
            var bytes = new byte[s.Stream.Length];
            var r = await s.Stream.ReadAsync(bytes);
            await File.WriteAllBytesAsync(CitiesDatabasePath, bytes);
            await CitiesDatabaseConnection.OpenAsync();
        }


        var w = Application.GetResourceStream(new Uri("/Assets/XiaomiWeather/xiaomi_weather_status.json",
            UriKind.Relative));
        if (w != null)
        {
            var codes = await JsonSerializer.DeserializeAsync<XiaomiWeatherStatusCodes>(w.Stream);
            WeatherStatusList = codes!.WeatherInfo;
        }
    }

    public async Task QueryWeatherAsync()
    {
        using var http = new HttpClient();
        var r = await http.GetAsync(
            $"https://weatherapi.market.xiaomi.com/wtr-v3/weather/all?latitude=110&longitude=112&locationKey=weathercn%3A{Settings.CityId}&days=15&appKey=weather20151024&sign=zUFJoAR2ZVrDy1vF3D07&isGlobal=false&locale=zh_cn");
        var str = await r.Content.ReadAsStringAsync();
        Settings.LastWeatherInfo = (await JsonSerializer.DeserializeAsync<WeatherInfo>(await r.Content.ReadAsStreamAsync()))!;
    }

    public string GetWeatherTextByCode(string code)
    {
        var c = (from i in WeatherStatusList 
                    where i.Code.ToString() == code
                    select i.Weather)
            .ToList();
        return c.Count > 0 ? c[0] : "未知";
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}