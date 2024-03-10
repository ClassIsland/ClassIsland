using System.Text.Json.Serialization;

namespace ClassIsland.Core.Models.Profile;

public class TimeLayoutItem : AttachableSettingsObject, IComparable
{
    private DateTime _startSecond = DateTime.Now;
    private DateTime _endSecond = DateTime.Now;
    private bool _isOnClass = true;
    private int _timeType = 0;
    private bool _isHideDefault = false;
    private string _defaultClassId = "";

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
            _startSecond = value;
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

    [JsonIgnore]
    public TimeSpan Last => EndSecond.TimeOfDay - StartSecond.TimeOfDay;

    /// <summary>
    /// 时间点类型
    /// </summary>
    /// <value>
    /// 0 - 上课 <br/>
    /// 1 - 课间 <br/>
    /// 2 - 分割线
    /// </value>
    public int TimeType
    {
        get => _timeType;
        set
        {
            if (value == _timeType) return;
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
            _isHideDefault = value;
            OnPropertyChanged();
        }
    }

    public string DefaultClassId
    {
        get => _defaultClassId;
        set
        {
            if (value == _defaultClassId) return;
            _defaultClassId = value;
            OnPropertyChanged();
        }
    }

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