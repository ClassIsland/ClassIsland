using System.Text.Json.Serialization;
using ClassIsland.Shared.Models.Action;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 代表一个<see cref="TimeLayout"/>中的时间点。
/// </summary>
public class TimeLayoutItem : AttachableSettingsObject, IComparable
{
    private DateTime _startSecond = DateTime.Now;
    private DateTime _endSecond = DateTime.Now;
    private bool _isOnClass = true;
    private int _timeType = 0;
    private bool _isHideDefault = false;
    private string _defaultClassId = "";
    private string _breakName = "";
    private ActionSet? _actionSet;

    /// <summary>
    /// 时间段在一天中开始的秒钟数
    /// </summary>
    public DateTime StartSecond
    {
        get => _startSecond;
        set
        {
            if (value == _startSecond) return;
            //EnsureTime(value, EndSecond);
            OnPropertyChanging();
            _startSecond = value;
            if (TimeType is 2 or 3)
            {
                EndSecond = value;
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(Last));
        }
    }

    /// <summary>
    /// 时间段在一天中结束的秒钟数
    /// </summary>
    public DateTime EndSecond
    {
        get => _endSecond;
        set
        {
            if (value == _endSecond) return;
            OnPropertyChanging();
            //EnsureTime(StartSecond, value);
            _endSecond = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Last));
        }
    }

    private void EnsureTime(DateTime start, DateTime end)
    {
        if (start.TimeOfDay > end.TimeOfDay)
        {
            throw new ArgumentException("时间点起始时间无效。");
        }
    }


    /// <summary>
    /// 代表一个空时间点。
    /// </summary>
    public static readonly TimeLayoutItem Empty = new()
    {
        StartSecond = DateTime.MinValue,
        EndSecond = DateTime.MinValue,
    };

    [JsonIgnore]
    public TimeSpan Last => EndSecond.TimeOfDay - StartSecond.TimeOfDay;

    /// <summary>
    /// 时间点类型
    /// </summary>
    /// <value>
    /// 0 - 上课 <br/>
    /// 1 - 课间 <br/>
    /// 2 - 分割线 <br/>
    /// 3 - 行动
    /// </value>
    public int TimeType
    {
        get => _timeType;
        set
        {
            if (value == _timeType) return;
            OnPropertyChanging();
            _timeType = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否默认隐藏
    /// </summary>
    public bool IsHideDefault
    {
        get => _isHideDefault;
        set
        {
            if (value == _isHideDefault) return;
            OnPropertyChanging();
            _isHideDefault = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 默认科目ID
    /// </summary>
    public string DefaultClassId
    {
        get => _defaultClassId;
        set
        {
            if (value == _defaultClassId) return;
            OnPropertyChanging();
            _defaultClassId = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 课间名称。
    /// </summary>
    [JsonIgnore]
    public string BreakNameText => string.IsNullOrEmpty(BreakName) ? "课间休息" : BreakName;

    /// <summary>
    /// 自定义课间名称。应使用<see cref="BreakNameText"/>获取实际课间名称。
    /// </summary>
    public string BreakName
    {
        get => _breakName;
        set
        {
            if (_breakName == value) return;
            OnPropertyChanging();
            _breakName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BreakNameText));
        }
    }

    /// <summary>
    /// 当当前时间点为【行动】时，要执行的行动组
    /// </summary>
    public ActionSet? ActionSet
    {
        get => _actionSet;
        set
        {
            if (Equals(value, _actionSet)) return;
            _actionSet = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 与另一个<see cref="TimeLayoutItem"/>比较
    /// </summary>
    /// <param name="obj">要比较的对象</param>
    /// <returns></returns>
    public int CompareTo(object? obj)
    {
        var o = (TimeLayoutItem?)obj;
        if (o is null)
        {
            return -1;
        }

        if (o.StartSecond.TimeOfDay < StartSecond.TimeOfDay)
        {
            return -1;
        }
        if (o.StartSecond.TimeOfDay > StartSecond.TimeOfDay)
        {
            return 1;
        }
        return 0;
    }
}