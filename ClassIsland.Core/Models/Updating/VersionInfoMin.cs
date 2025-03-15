using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Updating;

public class VersionInfoMin : ObservableRecipient
{
    private string _version = "";
    private string _title = "";
    private List<string> _channels = [];
    private DateTime _releaseTime = DateTime.MinValue;
    private string _versionInfoUrl = "";

    public string Version
    {
        get => _version;
        set
        {
            if (value == _version) return;
            _version = value;
            OnPropertyChanged();
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            if (value == _title) return;
            _title = value;
            OnPropertyChanged();
        }
    }

    public List<string> Channels
    {
        get => _channels;
        set
        {
            if (Equals(value, _channels)) return;
            _channels = value;
            OnPropertyChanged();
        }
    }

    public DateTime ReleaseTime
    {
        get => _releaseTime;
        set
        {
            if (value.Equals(_releaseTime)) return;
            _releaseTime = value;
            OnPropertyChanged();
        }
    }

    public string VersionInfoUrl
    {
        get => _versionInfoUrl;
        set
        {
            if (value == _versionInfoUrl) return;
            _versionInfoUrl = value;
            OnPropertyChanged();
        }
    }
}