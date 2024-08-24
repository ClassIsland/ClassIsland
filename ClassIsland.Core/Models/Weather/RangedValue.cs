using System.Text.Json.Serialization;

namespace ClassIsland.Core.Models.Weather;

public class RangedValue
{
    [JsonPropertyName("from")] public string From { get; set; } = "";
    [JsonPropertyName("to")] public string To { get; set; } = "";
}