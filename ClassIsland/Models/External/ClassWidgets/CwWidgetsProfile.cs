using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ClassIsland.Models.External.ClassWidgets;

public class CwWidgetsProfile
{
    [JsonPropertyName("widgets")]
    public List<string> Widgets { get; set; } = [];
}