using System.Text.Json.Serialization;

namespace ClassIsland.Core.Models.Weather;

public class StatusValueBase<T>
{
    [JsonPropertyName("value")] public T Value { get; set; } = default!;
}