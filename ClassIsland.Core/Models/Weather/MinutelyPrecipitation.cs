using System.Text.Json.Serialization;

namespace ClassIsland.Core.Models.Weather;

public class MinutelyPrecipitation
{
    [JsonPropertyName("value")] public List<double> Value { get; set; } = [];

    [JsonIgnore]
    public int RainRemainingMinutes
    {
        get
        {
            if (Value.Count <= 0)
            {
                return 0;
            }

            if (Value[0] > 0)
            {
                for (var i = 0; i < Value.Count; i++)
                {
                    if (Value[i] <= 0)
                    {
                        return -i;
                    }
                }
            }
            for (var i = 0; i < Value.Count; i++)
            {
                if (Value[i] > 0)
                {
                    return i;
                }
            }

            return 0;
        }
    }
}