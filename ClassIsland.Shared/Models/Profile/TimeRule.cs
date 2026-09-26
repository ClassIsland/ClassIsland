using CommunityToolkit.Mvvm.ComponentModel;

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 代表一个课表<see cref="ClassPlan"/>触发规则。
/// </summary>
public partial class TimeRule : ObservableRecipient
{
    /// <summary>
    /// 时间规则类型
    /// </summary>
    [ObservableProperty] [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [NotifyPropertyChangedFor(nameof(TypeString))]
    private TimeRuleType _type;

    /// <summary>
    /// 时间规则类型（字符串版）
    /// </summary>
    [JsonIgnore]
    public string TypeString => Type.ToString();

    /// <summary>
    /// 是否限制启用时间范围
    /// </summary>
    [ObservableProperty] private bool _restrictsEnableRange = false;

    // 能不能早日把 dnf 目标 drop 掉😭熬比版本毁我一生
    /// <summary>
    /// 限制启用时间起始日期
    /// </summary>
#if NET6_0_OR_GREATER
    [ObservableProperty] private DateOnly _rangeStart = DateOnly.FromDateTime(DateTime.Today);
#else
    [ObservableProperty] private string _rangeStart = DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
#endif

    /// <summary>
    /// 限制启用时间结束日期
    /// </summary>
#if NET6_0_OR_GREATER
    [ObservableProperty] private DateOnly _rangeEnd = DateOnly.FromDateTime(DateTime.Today);
#else
    [ObservableProperty] private string _rangeEnd = DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
#endif

#if NET6_0_OR_GREATER
    partial void OnRangeStartChanged(DateOnly value)
    {
        if (RangeEnd < value)
        {
            RangeEnd = value;
        }
    }

    partial void OnRangeEndChanged(DateOnly value)
    {
        if (RangeStart > value)
        {
            RangeStart = value;
        }
    }
#endif

    #region Weekly

    /// <summary>
    /// 在一周中的哪一天启用这个课表
    /// </summary>
    [ObservableProperty] 
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    private int _weekDay;
    
    /// <summary>
    /// 在多周轮换中的哪一周启用这个课表
    /// </summary>
    /// <value>
    /// 0 - 不轮换<br/>
    /// n - 第 n 周
    /// </value>
    [ObservableProperty]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    private int _weekCountDiv = 0;
    
    /// <summary>
    /// 多周轮换总周数
    /// </summary>
    /// <value>
    /// 2 - 双周轮换<br/>
    /// n - n周轮换<br/>
    /// </value>
    [ObservableProperty]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    private int _weekCountDivTotal = 2;
    
    #endregion

    #region Dete

    /// <summary>
    /// 要启用的日期
    /// </summary>
#if NET6_0_OR_GREATER
    [ObservableProperty] private ObservableCollection<DateOnly> _enableDates = [DateOnly.FromDateTime(DateTime.Today)];
#else
    [ObservableProperty] private ObservableCollection<string> _enableDates = [];
#endif

    #endregion

    #region Loop

    /// <summary>
    /// 循环启用周期长度
    /// </summary>
    [ObservableProperty] private int _loopCycleDays = 3;

    /// <summary>
    /// 循环偏移天数
    /// </summary>
    [ObservableProperty] 
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    private int _loopOffsetDays = 0;

    #endregion

    /// <summary>
    /// 时间规则类型
    /// </summary>
    public enum TimeRuleType
    {
        /// <summary>
        /// 每周启用
        /// </summary>
        Weekly,
        /// <summary>
        /// 特定日期启用
        /// </summary>
        Date,
        /// <summary>
        /// 循环启用
        /// </summary>
        Loop,
    }
}
