using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows.Documents;

namespace ClassIsland.Models;

public class XiaomiWeatherInfo
{
    // ReSharper disable once StringLiteralTypo
    [JsonPropertyName("weatherinfo")]
    public List<XiaomiWeatherInfoItem> WeatherInfo { get; set; } = new();
}