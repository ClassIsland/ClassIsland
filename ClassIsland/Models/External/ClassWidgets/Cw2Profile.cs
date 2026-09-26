using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClassIsland.Models.External.ClassWidgets;

internal sealed class Cw2Profile
{
    [JsonPropertyName("meta")]
    public Cw2Meta? Meta { get; set; }

    [JsonPropertyName("subjects")]
    public List<Cw2Subject>? Subjects { get; set; }

    [JsonPropertyName("days")]
    public List<Cw2Timeline>? Days { get; set; }

    [JsonPropertyName("overrides")]
    public List<Cw2Override>? Overrides { get; set; }
}

internal sealed class Cw2Meta
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("maxWeekCycle")]
    public int MaxWeekCycle { get; set; }

    [JsonPropertyName("startDate")]
    public string? StartDate { get; set; }
}

internal sealed class Cw2Subject
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("simplifiedName")]
    public string? SimplifiedName { get; set; }

    [JsonPropertyName("teacher")]
    public string? Teacher { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("isLocalClassroom")]
    public bool IsLocalClassroom { get; set; } = true;

    [JsonPropertyName("isLocalClassRoom")]
    public bool? LegacyIsLocalClassroom { get; set; }
}

internal sealed class Cw2Timeline
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("entries")]
    public List<Cw2Entry>? Entries { get; set; }

    [JsonPropertyName("dayOfWeek")]
    public JsonElement? DayOfWeek { get; set; }

    [JsonPropertyName("weeks")]
    public JsonElement? Weeks { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }
}

internal sealed class Cw2Entry
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("startTime")]
    public string? StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public string? EndTime { get; set; }

    [JsonPropertyName("subjectId")]
    public string? SubjectId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

internal sealed class Cw2Override
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("entryId")]
    public string? EntryId { get; set; }

    [JsonPropertyName("dayOfWeek")]
    public JsonElement? DayOfWeek { get; set; }

    [JsonPropertyName("weeks")]
    public JsonElement? Weeks { get; set; }

    [JsonPropertyName("subjectId")]
    public string? SubjectId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("startTime")]
    public string? StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public string? EndTime { get; set; }
}
