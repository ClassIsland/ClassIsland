using ClassIsland.Core.Enums.Updating;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Updating;

public class DownloadInfo : ObservableRecipient
{
    private DeployMethod _deployMethod = DeployMethod.None;
    private Dictionary<string, string> _archiveDownloadUrls = new();
    private string _archiveSha256 = "";
    private Dictionary<string, string> _seperatedFilesDownloadUrls = new();
    private Dictionary<string, string> _seperatedFilesHashes = new();
    private string? _requiredDotNetVersion;
    private string _requiredPlatformVersion = "";
    private string _requiredArch = "";

    public DeployMethod DeployMethod
    {
        get => _deployMethod;
        set
        {
            if (value == _deployMethod) return;
            _deployMethod = value;
            OnPropertyChanged();
        }
    }

    public Dictionary<string, string> ArchiveDownloadUrls
    {
        get => _archiveDownloadUrls;
        set
        {
            if (Equals(value, _archiveDownloadUrls)) return;
            _archiveDownloadUrls = value;
            OnPropertyChanged();
        }
    }

    public string ArchiveSHA256
    {
        get => _archiveSha256;
        set
        {
            if (value == _archiveSha256) return;
            _archiveSha256 = value;
            OnPropertyChanged();
        }
    }

    public Dictionary<string, string> SeperatedFilesDownloadUrls
    {
        get => _seperatedFilesDownloadUrls;
        set
        {
            if (Equals(value, _seperatedFilesDownloadUrls)) return;
            _seperatedFilesDownloadUrls = value;
            OnPropertyChanged();
        }
    }

    public Dictionary<string, string> SeperatedFilesHashes
    {
        get => _seperatedFilesHashes;
        set
        {
            if (Equals(value, _seperatedFilesHashes)) return;
            _seperatedFilesHashes = value;
            OnPropertyChanged();
        }
    }

    public string? RequiredDotNetVersion
    {
        get => _requiredDotNetVersion;
        set
        {
            if (value == _requiredDotNetVersion) return;
            _requiredDotNetVersion = value;
            OnPropertyChanged();
        }
    }

    public string RequiredPlatformVersion
    {
        get => _requiredPlatformVersion;
        set
        {
            if (value == _requiredPlatformVersion) return;
            _requiredPlatformVersion = value;
            OnPropertyChanged();
        }
    }

    public string RequiredArch
    {
        get => _requiredArch;
        set
        {
            if (value == _requiredArch) return;
            _requiredArch = value;
            OnPropertyChanged();
        }
    }
}