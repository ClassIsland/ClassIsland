using System;
using System.Text.Json.Serialization;

namespace ClassIsland.Models.Weather;

public class CurrentWeather
{
    [JsonPropertyName("feelsLike")] public ValueUnitPair FeelsLike { get; set; } = new();
    [JsonPropertyName("humidity")] public ValueUnitPair Humidity { get; set; } = new();

    [JsonPropertyName("pressure")] public ValueUnitPair Pressure { get; set; } = new();

    [JsonPropertyName("temperature")] public ValueUnitPair Temperature { get; set; } = new();

    [JsonPropertyName("visibility")] public ValueUnitPair Visibility { get; set; } = new();

    [JsonPropertyName("weather")] public string Weather { get; set; } = "99";

    [JsonPropertyName("pubTime")] public DateTime PublishTime { get; set; } = DateTime.Now;
}