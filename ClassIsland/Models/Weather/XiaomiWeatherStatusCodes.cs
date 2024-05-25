using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ClassIsland.Models.Weather;

public class XiaomiWeatherStatusCodes
{
    // ReSharper disable once StringLiteralTypo
    [JsonPropertyName("weatherinfo")]
    public List<XiaomiWeatherStatusCodeItem> WeatherInfo { get; set; } = new();
}