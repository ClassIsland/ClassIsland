using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClassIsland.Models.External.ClassWidgets;

public class CwProfile
{
    // Fuck CW
    [JsonPropertyName("part")]
    public Dictionary<string, List<object>> Part { get; set; } = new();

    [JsonPropertyName("part_name")]
    public Dictionary<string, string> PartName { get; set; } = new();
    
    [JsonPropertyName("timeline")]
    public Dictionary<string, List<List<JsonElement>>> Timeline { get; set; } = new();
    
    [JsonPropertyName("timeline_even")]
    public Dictionary<string, List<List<JsonElement>>> TimelineEven { get; set; } = new();
    
    [JsonPropertyName("schedule")]
    public Dictionary<string, List<string>> Schedule { get; set; } = new();
    
    [JsonPropertyName("schedule_even")]
    public Dictionary<string, List<string>> ScheduleEven { get; set; } = new();
}