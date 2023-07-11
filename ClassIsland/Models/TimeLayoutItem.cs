using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class TimeLayoutItem : ObservableRecipient
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

    public TimeSpan Last => EndSecond - StartSecond;

    /// <summary>
    /// 时间点类型
    /// </summary>
    /// <value>
    /// 0 - 上课
    /// 1 - 课间
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
}