using System.Text.Json.Serialization;

namespace ClassIsland.Models;

public class XiaomiWeatherInfoItem
{
    [JsonPropertyName("code")]
    public int Code { get; set; } = 99;

    [JsonPropertyName("wea")]
    public string Weather { get; set; } = "";
}