using System.Text.Json.Serialization;

namespace ClassIsland.Core.Models.Weather;

public class CurrentWeather
{
    [JsonPropertyName("feelsLike")] public ValueUnitPair FeelsLike { get; set; } = new();
    [JsonPropertyName("humidity")] public ValueUnitPair Humidity { get; set; } = new();

    [JsonPropertyName("pressure")] public ValueUnitPair Pressure { get; set; } = new();

    [JsonPropertyName("temperature")] public ValueUnitPair Temperature { get; set; } = new();

    [JsonPropertyName("visibility")] public ValueUnitPair Visibility { get; set; } = new();

    [JsonPropertyName("weather")] private string WeatherRaw { get; set; } = "99";

    [JsonIgnore] public string Weather => WeatherConverter();

    [JsonPropertyName("pubTime")] public DateTime PublishTime { get; set; } = DateTime.Now;

    [JsonPropertyName("wind")] public WindInfo Wind { get; set; } = new();

    private string WeatherConverter()
    {
        if (DateTime.Now.Hour >= 19 || DateTime.Now.Hour < 5)
        {
            if (WeatherRaw == "0")
            {
                return "10000";
            }
            else if (WeatherRaw == "1")
            {
                return "10001";
            }
            else
            {
                return WeatherRaw;
            }
        }
        else
        {
            return WeatherRaw;
        }
    }
}