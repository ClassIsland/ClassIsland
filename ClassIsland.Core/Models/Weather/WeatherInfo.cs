using System.Text.Json.Serialization;

namespace ClassIsland.Core.Models.Weather;

public class WeatherInfo
{

    [JsonPropertyName("current")] public CurrentWeather Current { get; set; } = new();

    [JsonPropertyName("alerts")] public List<WeatherAlert> Alerts { get; set; } = new();


    [JsonPropertyName("updateTime")] public long UpdateTimeUnix { get; set; } = 0;

    [JsonPropertyName("forecastDaily")] public ForecastDaily ForecastDaily { get; set; } = new();

    [JsonIgnore] public DateTime UpdateTime => DateTimeOffset.FromUnixTimeMilliseconds(UpdateTimeUnix).LocalDateTime;
}