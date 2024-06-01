using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 代表一个课表<see cref="ClassPlan"/>触发规则。
/// </summary>
public class TimeRule : ObservableRecipient
{
    private int _weekDay = new();
    private int _weekCountDiv = 0;

    public int WeekDay
    {
        get => _weekDay;
        set
        {
            if (Equals(value, _weekDay)) return;
            _weekDay = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 单周/双周
    /// </summary>
    /// <value>
    /// 0 - 不限<br/>
    /// 1 - 单周<br/>
    /// 2 - 双周
    /// </value>
    public int WeekCountDiv
    {
        get => _weekCountDiv;
        set
        {
            if (value == _weekCountDiv) return;
            _weekCountDiv = value;
            OnPropertyChanged();
        }
    }
}