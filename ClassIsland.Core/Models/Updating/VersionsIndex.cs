using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Updating;

public class VersionsIndex : ObservableRecipient
{
    private Dictionary<string, DownloadMirror> _mirrors = new();
    private List<VersionInfoMin> _versions = [];
    private Dictionary<string, ChannelInfo> _channels = [];

    public Dictionary<string, DownloadMirror> Mirrors
    {
        get => _mirrors;
        set
        {
            if (Equals(value, _mirrors)) return;
            _mirrors = value;
            OnPropertyChanged();
        }
    }

    public List<VersionInfoMin> Versions
    {
        get => _versions;
        set
        {
            if (Equals(value, _versions)) return;
            _versions = value;
            OnPropertyChanged();
        }
    }

    public Dictionary<string, ChannelInfo> Channels
    {
        get => _channels;
        set
        {
            if (Equals(value, _channels)) return;
            _channels = value;
            OnPropertyChanged();
        }
    }
}