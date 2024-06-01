using System.Text.Json.Serialization;

namespace ClassIsland.Core.Models.Weather;

public class XiaomiWeatherStatusCodes
{
    // ReSharper disable once StringLiteralTypo
    [JsonPropertyName("weatherinfo")]
    public List<XiaomiWeatherStatusCodeItem> WeatherInfo { get; set; } = new();
}