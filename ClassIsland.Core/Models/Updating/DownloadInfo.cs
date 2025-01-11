using ClassIsland.Core.Enums.Updating;

namespace ClassIsland.Core.Models.Updating;

public class DownloadInfo
{
    public DeployMethod DeployMethod { get; set; } = DeployMethod.None;

    public Dictionary<string, string> ArchiveDownloadUrls { get; set; } = new();

    public string ArchiveSHA256 { get; set; } = "";

    public Dictionary<string, string> SeperatedFilesDownloadUrls { get; set; } = new();

    public Dictionary<string, string> SeperatedFilesHashes { get; set; } = new();

    public string? RequiredDotNetVersion { get; set; }

    public string RequiredPlatformVersion { get; set; } = "";

    public string RequiredArch { get; set; } = "";
}