using System.Text.Json.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 代表一个在<see cref="ClassPlan"/>中的课程。
/// </summary>
public class ClassInfo : AttachableSettingsObject
{
    private string _subjectId = "";
    private int _index = 0;
    private TimeLayout _currentTimeLayout = new();
    private bool _isChangedClass = false;
    private bool _isEnabled = true;

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
    public string SubjectId
    {
        get => _subjectId;
        set
        {
            if (value == _subjectId) return;
            _subjectId = value ?? "";
            OnPropertyChanged();
        }
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
}