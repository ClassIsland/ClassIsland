namespace ClassIsland.Core.Models.Updating;

public class VersionInfo : VersionInfoMin
{
    private Dictionary<string, DownloadInfo> _downloadInfos = new();
    private string _changeLogs = "";

    public Dictionary<string, DownloadInfo> DownloadInfos
    {
        get => _downloadInfos;
        set
        {
            if (Equals(value, _downloadInfos)) return;
            _downloadInfos = value;
            OnPropertyChanged();
        }
    }

    public string ChangeLogs
    {
        get => _changeLogs;
        set
        {
            if (value == _changeLogs) return;
            _changeLogs = value;
            OnPropertyChanged();
        }
    }
}