using CommunityToolkit.Mvvm.ComponentModel;

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 代表一个课表<see cref="ClassPlan"/>触发规则。
/// </summary>
public class TimeRule : ObservableRecipient
{
    private int _weekDay = new();
    private int _weekCountDiv = 0;
    private int _weekCountDivTotal = 2;

    /// <summary>
    /// 在一周中的哪一天启用这个课表
    /// </summary>
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
    /// 在多周轮换中的哪一周启用这个课表
    /// </summary>
    /// <value>
    /// 0 - 不轮换<br/>
    /// n - 第 n 周
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

    /// <summary>
    /// 多周轮换总周数
    /// </summary>
    /// <value>
    /// 2 - 双周轮换<br/>
    /// n - n周轮换<br/>
    /// </value>
    public int WeekCountDivTotal
    {
        get => _weekCountDivTotal;
        set
        {
            if (value == _weekCountDivTotal) return;
            // if (WeekCountDiv > WeekCountDivTotal)
            // {
            //     WeekCountDiv = 0;
            // }
            _weekCountDivTotal = value;
            OnPropertyChanged();
        }
    }
}