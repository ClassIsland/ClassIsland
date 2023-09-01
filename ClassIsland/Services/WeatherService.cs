using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
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

    private DispatcherTimer UpdateTimer { get; } = new()
    {
        Interval = TimeSpan.FromMinutes(15)
    };

    public WeatherService(SettingsService settingsService, FileFolderService fileFolderService, IHostApplicationLifetime hostApplicationLifetime)
    {
        SettingsService = settingsService;
        hostApplicationLifetime.ApplicationStopping.Register(AppStopping);
        LoadData();
        UpdateTimer.Tick += UpdateTimerOnTick;
        UpdateTimer.Start();
    }

    private async void UpdateTimerOnTick(object? sender, EventArgs e)
    {
        await QueryWeatherAsync();
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
        try
        {
            using var http = new HttpClient();
            var r = await http.GetAsync(
                $"https://weatherapi.market.xiaomi.com/wtr-v3/weather/all?latitude=110&longitude=112&locationKey=weathercn%3A{Settings.CityId}&days=15&appKey=weather20151024&sign=zUFJoAR2ZVrDy1vF3D07&isGlobal=false&locale=zh_cn");
            var str = await r.Content.ReadAsStringAsync();
            Settings.LastWeatherInfo = JsonSerializer.Deserialize<WeatherInfo>(str)!;
        }
        catch
        {
            // ignored
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

    public List<City> GetCitiesByName(string name)
    {
        var cmd = CitiesDatabaseConnection.CreateCommand();
        cmd.CommandText = @"
            SELECT name, city_num
            FROM citys
            WHERE name LIKE $name
            ";
        cmd.Parameters.AddWithValue("name", $"%{name}%");
        using var reader = cmd.ExecuteReader();
        var l = new List<City>();
        while (reader.Read())
        {
            l.Add(new City()
            {
                Name = reader.GetString(0),
                CityId = reader.GetString(1)
            });
        }
        return l;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}