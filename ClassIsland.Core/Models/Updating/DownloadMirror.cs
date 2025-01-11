using System.Text.Json.Serialization;

namespace ClassIsland.Core.Models.Updating;

public class DownloadMirror
{
    public string Name { get; set; } = "";

    public List<string> SpeedTestUrls { get; set; } = [];

    [JsonIgnore]
    public SpeedTestResult SpeedTestResult { get; set; } = new();
}