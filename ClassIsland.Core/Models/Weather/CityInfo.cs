namespace ClassIsland.Core.Models.Weather;
using System.Text.Json.Serialization;

/// <summary>
/// CityInfo 搜索城市或地区的信息。
/// </summary>
public class CityInfo
{
    /// <summary>
    /// Name 城市或地区名称。
    /// </summary>
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Affiliation 城市或地区所属的国家或地区。
    /// </summary>
    [JsonPropertyName("affiliation")] public string Affiliation { get; set; } = string.Empty;
    
    /// <summary>
    /// LocationKey 城市或地区的唯一标识符。
    /// </summary>
    [JsonPropertyName("locationKey")] public string LocationKey { get; set; } = string.Empty;

    /// <summary>
    /// Longitude 城市或地区的经度。
    /// </summary>
    [JsonPropertyName("latitude")] public string Latitude { get; set; } = string.Empty;
    
    /// <summary>
    /// Latitude 城市或地区的纬度。
    /// </summary>
    [JsonPropertyName("longitude")] public string Longitude { get; set; } = string.Empty;
}