using System.Text.Json.Serialization;

namespace ClassIsland.Core.Models.Weather;

public class Minutely
{
    [JsonPropertyName("precipitation")] public MinutelyPrecipitation Precipitation { get; set; } = new();
}