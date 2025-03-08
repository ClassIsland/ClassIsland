using ClassIsland.Core.Enums.Metadata.Announcement;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Metadata.Announcement;

/// <summary>
/// 代表一个应用公告。
/// </summary>
public class Announcement : ObservableRecipient
{
    private string _summary = "";
    private string _details = "";
    private bool _hasDetails = false;
    private bool _detailsOpenUri = false;
    private string _detailsUri = "";
    private DateTime _startTime = DateTime.MinValue;
    private DateTime _endTime = DateTime.MinValue;
    private Guid _guid = Guid.NewGuid();
    private ReadStateStorageScope _readStateStorageScope = ReadStateStorageScope.Local;
    private Severity _severity = Severity.Announcement;

    /// <summary>
    /// 公告摘要
    /// </summary>
    public string Summary
    {
        get => _summary;
        set
        {
            if (value == _summary) return;
            _summary = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 公告详细信息（Markdown）
    /// </summary>
    public string Details
    {
        get => _details;
        set
        {
            if (value == _details) return;
            _details = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 公告是否有详细信息
    /// </summary>

    public bool HasDetails
    {
        get => _hasDetails;
        set
        {
            if (value == _hasDetails) return;
            _hasDetails = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 公告详细按钮是否是打开 Uri
    /// </summary>
    public bool DetailsOpenUri
    {
        get => _detailsOpenUri;
        set
        {
            if (value == _detailsOpenUri) return;
            _detailsOpenUri = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 详细按钮打开的 Uri
    /// </summary>
    public string DetailsUri
    {
        get => _detailsUri;
        set
        {
            if (value == _detailsUri) return;
            _detailsUri = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 公告开始展示时间
    /// </summary>
    public DateTime StartTime
    {
        get => _startTime;
        set
        {
            if (value.Equals(_startTime)) return;
            _startTime = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 公告结束展示时间
    /// </summary>
    public DateTime EndTime
    {
        get => _endTime;
        set
        {
            if (value.Equals(_endTime)) return;
            _endTime = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 公告 GUID
    /// </summary>
    public Guid Guid
    {
        get => _guid;
        set
        {
            if (value.Equals(_guid)) return;
            _guid = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 公告已读状态存储范围
    /// </summary>
    public ReadStateStorageScope ReadStateStorageScope
    {
        get => _readStateStorageScope;
        set
        {
            if (value == _readStateStorageScope) return;
            _readStateStorageScope = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 公告严重程度
    /// </summary>
    public Severity Severity
    {
        get => _severity;
        set
        {
            if (value == _severity) return;
            _severity = value;
            OnPropertyChanged();
        }
    }
}