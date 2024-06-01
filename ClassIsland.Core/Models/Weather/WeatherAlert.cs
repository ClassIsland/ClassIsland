using System.Text.Json.Serialization;

namespace ClassIsland.Core.Models.Weather;

public class WeatherAlert
{
    [JsonPropertyName("locationKey")] public string LocationKey { get; set; } = "";
    [JsonPropertyName("alertId")] public string AlertId { get; set; } = "";

    [JsonPropertyName("pubTime")] public DateTime PubTime { get; set; } = DateTime.Now;
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("level")] public string Level { get; set; } = "";
    [JsonPropertyName("detail")] public string Detail { get; set; } = "";
    [JsonPropertyName("images")] public Dictionary<string, string> Images { get; set; } = new();
}