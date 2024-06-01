using System.Text.Json.Serialization;

namespace ClassIsland.Core.Models.Weather;

public class ValueUnitPair
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = "";

    [JsonPropertyName("unit")]

    public string Unit { get; set; } = "";

    public override string ToString()
    {
        return $"{Value}{Unit}";
    }
}