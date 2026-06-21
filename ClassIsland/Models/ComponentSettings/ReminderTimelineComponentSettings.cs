using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.ComponentSettings;

public class ReminderTimelineComponentSettings : ObservableRecipient
{
    private int _groupsBefore = 5;
    private int _groupsAfter = 5;
    private int _maxPerGroup = 5;

    /// <summary>
    /// 当前时间之前（已过期）最多显示多少个分组（同一分钟为一个分组）
    /// </summary>
    public int GroupsBefore
    {
        get => _groupsBefore;
        set
        {
            if (value == _groupsBefore) return;
            _groupsBefore = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 当前时间之后（未过期）最多显示多少个分组（同一分钟为一个分组）
    /// </summary>
    public int GroupsAfter
    {
        get => _groupsAfter;
        set
        {
            if (value == _groupsAfter) return;
            _groupsAfter = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 每个分组内最多显示多少个日程，超出部分显示"等"字样
    /// </summary>
    public int MaxPerGroup
    {
        get => _maxPerGroup;
        set
        {
            if (value == _maxPerGroup) return;
            _maxPerGroup = value;
            OnPropertyChanged();
        }
    }
}
