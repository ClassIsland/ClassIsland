namespace ClassIsland.Core.Models.Updating;

public class VersionInfo : VersionInfoMin
{
    public Dictionary<string, DownloadInfo> DownloadInfos { get; set; } = new();

    public string ChangeLogs { get; set; } = "";

}