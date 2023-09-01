using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ClassIsland.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services;

public class WeatherService : IHostedService
{
    public const string CitiesDatabasePath = "./Temp/xiaomi_weather.db";

    private SettingsService SettingsService { get; }

    private Settings Settings => SettingsService.Settings;

    public SqliteConnection CitiesDatabaseConnection { get; } = new($"Data Source={CitiesDatabasePath}");

    public List<XiaomiWeatherInfoItem> WeatherStatusList { get; set; } = new();

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
            var codes = await JsonSerializer.DeserializeAsync<XiaomiWeatherInfo>(w.Stream);
            WeatherStatusList = codes!.WeatherInfo;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}