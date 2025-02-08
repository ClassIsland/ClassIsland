using System.Text.Json.Serialization;

namespace ClassIsland.Core.Models.Weather;

public class ForecastHourly
{
    [JsonPropertyName("temperature")] public StatusValueBase<List<int>> Temperature { get; set; } = new();
    [JsonPropertyName("weather")] public StatusValueBase<List<int>> Weather { get; set; } = new();
}