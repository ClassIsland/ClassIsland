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
    public int IsDefaultIcon => Images["icon"]
        is "http://f5.market.xiaomi.com/download/Weather/0ac110d2ee20a454ab44f5df30f9fa6ff650e0b72/a.webp" // 蓝色预警默认图标
        or "http://f4.market.mi-img.com/download/Weather/072013febeb1944da85649e5e547ec5a8284816a2/a.webp" // 黄色预警默认图标
        or "http://f5.market.xiaomi.com/download/Weather/06db501333e6d4075a3364a66cdf23ba5733111b3/a.webp" // 橙色预警默认图标
        or "http://f3.market.xiaomi.com/download/Weather/03e3e096d3d9e485fa33bbf833fc3b3c96c23d014/a.webp" // 红色预警默认图标
        ? 1 : 0;
}