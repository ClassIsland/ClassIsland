using System.Text.Json.Serialization;

namespace ClassIsland.Core.Models.Weather;

public class ForecastDaily
{
    [JsonPropertyName("precipitationProbability")]
    public StatusValueBase<List<string>> PrecipitationProbability { get; set; } = new();

    [JsonPropertyName("temperature")] public StatusValueBase<List<RangedValue>> Temperature { get; set; } = new();
    [JsonPropertyName("weather")] public StatusValueBase<List<RangedValue>> Weather { get; set; } = new();
}