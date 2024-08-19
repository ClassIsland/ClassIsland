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
    private ObservableCollection<string> _weekCountDivs = [];

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
    /// 1 - 第一周<br/>
    /// 2 - 第二周<br/>
    /// n - 第n周
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
    /// 3 - 三周轮换<br/>
    /// 4 - 四周轮换
    /// </value>
    public int WeekCountDivTotal
    {
        get => _weekCountDivTotal;
        set
        {
            if (value == _weekCountDivTotal) return;
            _weekCountDivTotal = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 多周轮换选择框的项目列表
    /// </summary>
    [JsonIgnore]
    public ObservableCollection<string> WeekCountDivs
    {
        get => _weekCountDivs;
        set
        {
            if (value == _weekCountDivs) return;
            _weekCountDivs = value;
            OnPropertyChanged();
        }
    }
}