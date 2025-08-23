using System.Diagnostics;
using System.Text.Json.Serialization;
using ClassIsland.Shared.JsonConverters;
using ClassIsland.Shared.Models.Automation;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 代表一个<see cref="TimeLayout"/>中的时间点。
/// </summary>
public class TimeLayoutItem : AttachableSettingsObject, IComparable
{
    private string _startSecond = "";
    private string _endSecond =  "";
    private int _timeType = 0;
    private bool _isHideDefault = false;
    private Guid _defaultClassId = Guid.Empty;
    private string _breakName = "";
    private ActionSet? _actionSet;
    private TimeSpan _startTime = TimeSpan.Zero;
    private TimeSpan _endTime = TimeSpan.Zero;

    /// <summary>
    /// 时间段在一天中开始的秒钟数
    /// </summary>
    [Obsolete("请使用 StartTime 属性。", true)]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string StartSecond
    {
        get => _startSecond;
        set
        {
            if (value == _startSecond) return;
            OnPropertyChanging();
            _startSecond = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 时间段在一天中结束的秒钟数
    /// </summary>
    [Obsolete("请使用 EndTime 属性。", true)]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string EndSecond
    {
        get => _endSecond;
        set
        {
            if (value == _endSecond) return;
            OnPropertyChanging();
            //EnsureTime(StartSecond, value);
            _endSecond = value;
            OnPropertyChanged();

        }
    }

    /// <summary>
    /// 时间点在一天中开始的时间
    /// </summary>
    public TimeSpan StartTime
    {
        get => _startTime;
        set
        {
            if (value.Equals(_startTime)) return;
            OnPropertyChanging();
            _startTime = value;
            if (TimeType is 2 or 3)
            {
                EndTime = value;
            }
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 时间点在一天中结束的时间
    /// </summary>
    public TimeSpan EndTime
    {
        get => _endTime;
        set
        {
            if (value.Equals(_endTime)) return;
            OnPropertyChanging();
            _endTime = value;
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
        StartTime = TimeSpan.Zero,
        EndTime = TimeSpan.Zero,
    };

    /// <summary>
    /// 时间点持续时间
    /// </summary>
    [JsonIgnore]
    public TimeSpan Last => EndTime - StartTime;

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
    [JsonConverter(typeof(GuidEmptyFallbackConverter))]
    public Guid DefaultClassId
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
    /// 自定义课间名称，可能为空。通过 <see cref="BreakNameText"/> 获取实际显示的课间名称。
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

        if (o.StartTime < StartTime)
        {
            return -1;
        }
        if (o.StartTime > StartTime)
        {
            return 1;
        }
        return 0;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var timeTypeText = TimeType switch
        {
            0 => "上课",
            1 => "课间休息",
            2 => "分割线",
            3 => "行动",
            _ => "？？？"
        };
        return $"{timeTypeText} {StartTime}-{EndTime}";
    }
}