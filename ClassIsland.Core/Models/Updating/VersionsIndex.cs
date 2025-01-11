namespace ClassIsland.Core.Models.Updating;

public class VersionsIndex
{
    public Dictionary<string, DownloadMirror> Mirrors { get; set; } = new();

    public List<VersionInfoMin> Versions { get; set; } = [];

    public Dictionary<string, ChannelInfo> Channels { get; set; } = [];

}