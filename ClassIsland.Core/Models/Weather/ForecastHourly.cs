using System.Text.Json.Serialization;

namespace ClassIsland.Core.Models.Weather;

public class ForecastHourly
{
    [JsonPropertyName("temperature")] public StatusValueBase<List<int>> Temperature { get; set; } = new();
    [JsonPropertyName("weather")] private StatusValueBase<List<int>> WeatherRaw { get; set; } = new();
    [JsonIgnore] public StatusValueBase<List<int>> Weather => WeatherConverter();

    private StatusValueBase<List<int>> WeatherConverter()
    {
        int Hour = DateTime.Now.Hour;
        for (var i = (Hour >= 19 ? 0 : 18 - Hour); i < 29 - Hour; i++)
        {
            if (WeatherRaw.Value[i] == "0")
            {
                WeatherRaw.Value[i] = "10000";
            }
            else if (WeatherRaw.Value[i] == "1")
            {
                WeatherRaw.Value[i] = "10000";
            }
        }
        for (var i = (Hour > 19 ? 42 - Hour : 23); i < 23 /* WeatherRaw.Value.Count */ ; i++)
        {
            if (WeatherRaw.Value[i] == "0")
            {
                WeatherRaw.Value[i] = "10000";
            }
            else if (WeatherRaw.Value[i] == "1")
            {
                WeatherRaw.Value[i] = "10000";
            }
        }
    }
}