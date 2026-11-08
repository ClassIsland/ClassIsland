using System.Text.Json.Serialization;
using System.ComponentModel;
using ClassIsland.Shared.JsonConverters;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 代表一个在<see cref="ClassPlan"/>中的课程。
/// </summary>
public class ClassInfo : AttachableSettingsObject
{
    private Guid _subjectId = Guid.Empty;
    private int _index = 0;
    private TimeLayout _currentTimeLayout = new();
    private bool _isChangedClass = false;
    private bool _isEnabled = true;
    private string _location = "";
    private string _effectiveLocation = "";
    private Subject? _subject;

    /// <summary>
    /// 课程在课程表中的位置
    /// </summary>
    [JsonIgnore]
    public int Index
    {
        get => _index;
        set
        {
            if (value == _index) return;
            _index = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentTimeLayoutItem));
        }
    }

    /// <summary>
    /// 课程所在课表的时间表
    /// </summary>
    [JsonIgnore]
    public TimeLayout CurrentTimeLayout
    {
        get => _currentTimeLayout;
        set
        {
            if (Equals(value, _currentTimeLayout)) return;
            _currentTimeLayout = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentTimeLayoutItem));
        }
    }

    /// <summary>
    /// 课程对应的时间点
    /// </summary>
    [JsonIgnore] public TimeLayoutItem CurrentTimeLayoutItem
    {
        get
        {
            var valid = (from i in CurrentTimeLayout.Layouts where i.TimeType == 0 select i).ToList();
            if (Index < 0 || Index >= valid.Count)
            {
                return TimeLayoutItem.Empty;
            }
            return valid[Index];
        }
    }


    /// <summary>
    /// 课程ID
    /// </summary>
    [JsonConverter(typeof(GuidEmptyFallbackConverter))]
    public Guid SubjectId
    {
        get => _subjectId;
        set
        {
            if (value == _subjectId) return;
            _subjectId = value;
            Subject = null;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 课程的上课地点覆盖值。留空时使用科目设置的上课地点。
    /// </summary>
    public string Location
    {
        get => _location;
        set
        {
            value ??= "";
            if (value == _location) return;
            _location = value;
            OnPropertyChanged();
            RefreshEffectiveLocation();
        }
    }

    /// <summary>
    /// 课程最终使用的上课地点。
    /// </summary>
    [JsonIgnore]
    public string EffectiveLocation => _effectiveLocation;

    /// <summary>
    /// 课程对应的科目。该引用不参与序列化。
    /// </summary>
    [JsonIgnore]
    public Subject? Subject
    {
        get => _subject;
        set
        {
            if (ReferenceEquals(value, _subject)) return;
            if (_subject != null)
            {
                _subject.PropertyChanged -= SubjectOnPropertyChanged;
            }

            _subject = value;
            if (_subject != null)
            {
                _subject.PropertyChanged += SubjectOnPropertyChanged;
            }

            RefreshEffectiveLocation();
            OnPropertyChanged();
        }
    }

    private void SubjectOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or nameof(Subject.Location))
        {
            RefreshEffectiveLocation();
        }
    }

    private void RefreshEffectiveLocation()
    {
        var location = string.IsNullOrWhiteSpace(Location)
            ? Subject?.Location ?? ""
            : Location;
        if (location == _effectiveLocation) return;
        _effectiveLocation = location;
        OnPropertyChanged(nameof(EffectiveLocation));
    }

    /// <summary>
    /// 课程是否是临时换课课程
    /// </summary>
    public bool IsChangedClass
    {
        get => _isChangedClass;
        set
        {
            if (value == _isChangedClass) return;
            _isChangedClass = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 这节课是否已启用
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (value == _isEnabled) return;
            _isEnabled = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// 当前<see cref="ClassInfo"/>是否为空
    /// </summary>
    [JsonIgnore] public bool IsEmpty { get; private set; } = false;

    /// <summary>
    /// 代表一个空的<see cref="ClassInfo"/>
    /// </summary>
    public static ClassInfo Empty { get; } = new()
    {
        IsEmpty = true
    };
}
