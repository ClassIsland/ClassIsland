using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class TimeLayoutItem : ObservableRecipient, IComparable
{
    private DateTime _startSecond = DateTime.Now;
    private DateTime _endSecond = DateTime.Now;
    private bool _isOnClass = true;
    private int _timeType = 0;

    /// <summary>
    /// 时间段在一天中开始的秒钟数
    /// </summary>
    public DateTime StartSecond
    {
        get => _startSecond;
        set
        {
            if (value == _startSecond) return;
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
            _endSecond = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Last));
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