using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class TimeLayoutItem : ObservableRecipient
{
    private long _startSecond;
    private int _endSecond;
    private bool _isOnClass = true;

    /// <summary>
    /// 时间段在一天中开始的秒钟数
    /// </summary>
    public long StartSecond
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
    public int EndSecond
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

    public TimeSpan Last => TimeSpan.FromSeconds(EndSecond - StartSecond);

    public bool IsOnClass
    {
        get => _isOnClass;
        set
        {
            if (value == _isOnClass) return;
            _isOnClass = value;
            OnPropertyChanged();
        }
    }
}