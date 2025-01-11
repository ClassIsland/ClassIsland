namespace ClassIsland.Core.Models.Updating;

public class VersionInfoMin
{
    public string Version { get; set; } = "";

    public string Title { get; set; } = "";

    public List<string> Channels { get; set; } = [];

    public DateTime ReleaseTime { get; set; } = DateTime.MinValue;

    public string VersionInfoUrl { get; set; } = "";
}