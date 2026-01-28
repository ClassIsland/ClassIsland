using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Updating;

/// <summary>
/// 用于更新服务的简略版本信息模型
/// </summary>
public class VersionInfoMin : ObservableRecipient
{
    private string _version = "";
    private string _title = "";
    private List<string> _channels = [];
    private DateTime _releaseTime = DateTime.MinValue;
    private string _versionInfoUrl = "";

    /// <summary>
    /// 版本号
    /// </summary>
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

    /// <summary>
    /// 版本标题
    /// </summary>
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

    /// <summary>
    /// 该版本的更新频道
    /// </summary>
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

    /// <summary>
    /// 该版本的发布时间
    /// </summary>
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

    /// <summary>
    /// 指向该版本更详细更新信息的URL
    /// </summary>
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