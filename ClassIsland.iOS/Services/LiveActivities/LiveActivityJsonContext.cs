using System.Text.Json.Serialization;

namespace ClassIsland.iOS.Services.LiveActivities;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSerializable(typeof(NativeLessonLiveActivityPayload))]
internal partial class LiveActivityJsonContext : JsonSerializerContext;

internal sealed record NativeLessonLiveActivityPayload(
    string IntervalId,
    int Phase,
    string Title,
    string Subtitle,
    string Detail,
    string CompactText,
    string? StartTime,
    string? EndTime,
    string DeepLink);
