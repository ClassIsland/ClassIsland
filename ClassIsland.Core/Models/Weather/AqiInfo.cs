using System.Text.Json.Serialization;

namespace ClassIsland.Core.Models.Weather;

public class AqiInfo
{
    [JsonPropertyName("aqi")] public string Aqi { get; set; } = "0.0";

    [JsonIgnore] public int AqiLevel => GetAqi();

    private int GetAqi()
    {
        var aqi = double.TryParse(Aqi, out var r) ? r : 0.0;
        switch (aqi)
        {
            case <= 50:
                return 1;
            case > 50 and <= 100:
                return 2;
            case > 100 and <= 150:
                return 3;
            case > 150 and <= 200:
                return 4;
            case > 200 and <= 300:
                return 5;
            case > 300:
                return 6;
            default:
                return -1;
        }
    }
}