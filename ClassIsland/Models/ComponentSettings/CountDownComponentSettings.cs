using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.ComponentSettings;

public class CountDownComponentSettings : ObservableRecipient
{
    private string _countDownName = "倒计时";
    private DateTime _overTime = DateTime.Now.Date;
    private int _daysLeft = 0;
    private Color _fontColor = Color.FromArgb(255, 255,0,0);
    private int _fontSize = 16;
    private bool _isCompactModeEnabled = false;
    private string _countDownConnector = "还有";
    private bool _isConnectorColorEmphasized = false;
    private DateTime _startTime = DateTime.Now.Date;
    private bool _useAccentOnProgressBar = true;
    private int _progressBarMode = 0;
    private bool _showProgress = false;
    private bool _isProgressInverted = false;
    private string _customStringFormat = "%D天";
    private int _countdownSource = 0;
    private DateTime _cycleStartTime = DateTime.Now;
    private TimeSpan _cycleBeforeDuration = TimeSpan.Zero;
    private TimeSpan _cycleDuration = TimeSpan.FromDays(1);
    private TimeSpan _cycleAfterDuration = TimeSpan.Zero;
    private bool _isCycleCountLimited = false;
    private int _cycleCountLimit = 2;
    private bool _isAdvancedCycleTimingEnabled = false;
    private int _natureTimeUseMode = 0;
    private DayOfWeek _weekCountdownStartDay = DayOfWeek.Monday;
    private bool _isCustomWeekCountdownStartDayEnabled = false;

    public string CountDownName
    {
        get => _countDownName;
        set
        {
            if (value == null) return;
            if (value.Equals(_countDownName)) return;
            _countDownName = value;
            OnPropertyChanged();
        }
    }

    public DateTime OverTime
    {
        get => _overTime;
        set
        {
            if (value.Equals(_overTime)) return;
            _overTime = value;
            OnPropertyChanged();
        }
    }

    public int DaysLeft
    {
        get => _daysLeft;
        set
        {
            if (value == _daysLeft) return;
            _daysLeft = value > 0 ? value : 0;
            OnPropertyChanged();
        }
    }

    public int FontSize
    {
        get => _fontSize;
        set
        {
            if (value.Equals(_fontSize)) return;
            _fontSize = value;
            OnPropertyChanged();
        }
    }

    public bool IsCompactModeEnabled
    {
        get => _isCompactModeEnabled;
        set
        {
            if (value == _isCompactModeEnabled) return;
            _isCompactModeEnabled = value;
            OnPropertyChanged();
        }
    }

    public string CountDownConnector
    {
        get => _countDownConnector;
        set
        {
            if (value == null) return;
            if (value.Equals(_countDownConnector)) return;
            _countDownConnector = value;
            OnPropertyChanged();
        }
    }

    public bool IsConnectorColorEmphasized
    {
        get => _isConnectorColorEmphasized;
        set
        {
            if (value == _isConnectorColorEmphasized) return;
            _isConnectorColorEmphasized = value;
            OnPropertyChanged();
        }
    }

    public Color FontColor
    {
        get => _fontColor;
        set
        {
            if (value.Equals(_fontColor)) return;
            _fontColor = value;
            OnPropertyChanged();
        }
    }

    public DateTime StartTime
    {
        get => _startTime;
        set
        {
            if (value.Equals(_startTime)) return;
            _startTime = value;
            OnPropertyChanged();
        }
    }

    public bool UseAccentOnProgressBar
    {
        get => _useAccentOnProgressBar;
        set
        {
            if (value == _useAccentOnProgressBar) return;
            _useAccentOnProgressBar = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 0 - 进度环;1 - 进度条
    /// </summary>
    public int ProgressBarMode
    {
        get => _progressBarMode;
        set
        {
            if (value == _progressBarMode) return;
            _progressBarMode = value;
            OnPropertyChanged();
        }
    }

    public bool ShowProgress
    {
        get => _showProgress;
        set
        {
            if (value == _showProgress) return;
            _showProgress = value;
            OnPropertyChanged();
        }
    }

    public bool IsProgressInverted
    {
        get => _isProgressInverted;
        set
        {
            if (value == _isProgressInverted) return;
            _isProgressInverted = value;
            OnPropertyChanged();
        }
    }

    public string CustomStringFormat
    {
        get => _customStringFormat;
        set
        {
            if (value == _customStringFormat) return;
            _customStringFormat = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 倒计时来源
    /// 0 - 固定时间
    /// 1 - 周期
    /// 2 - 今天
    /// 3 - 本周
    /// </summary>
    public int CountdownSource
    {
        get => _countdownSource;
        set
        {
            if (value == _countdownSource) return;
            _countdownSource = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 周期开始时间
    /// </summary>
    public DateTime CycleStartTime
    {
        get => _cycleStartTime;
        set
        {
            if (value.Equals(_cycleStartTime)) return;
            _cycleStartTime = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 单个周期前时长
    /// </summary>
    public TimeSpan CycleBeforeDuration
    {
        get => _cycleBeforeDuration;
        set
        {
            if (value.Equals(_cycleBeforeDuration)) return;
            _cycleBeforeDuration = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 单个周期计时时长
    /// </summary>
    public TimeSpan CycleDuration
    {
        get => _cycleDuration;
        set
        {
            if (value.Equals(_cycleDuration)) return;
            _cycleDuration = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 单个周期后时长
    /// </summary>
    public TimeSpan CycleAfterDuration
    {
        get => _cycleAfterDuration;
        set
        {
            if (value.Equals(_cycleAfterDuration)) return;
            _cycleAfterDuration = value;
            OnPropertyChanged();
        }
    }

    public bool IsCycleCountLimited
    {
        get => _isCycleCountLimited;
        set
        {
            if (value == _isCycleCountLimited) return;
            _isCycleCountLimited = value;
            OnPropertyChanged();
        }
    }

    public int CycleCountLimit
    {
        get => _cycleCountLimit;
        set
        {
            if (value == _cycleCountLimit) return;
            _cycleCountLimit = value;
            OnPropertyChanged();
        }
    }

    public bool IsAdvancedCycleTimingEnabled
    {
        get => _isAdvancedCycleTimingEnabled;
        set
        {
            if (value == _isAdvancedCycleTimingEnabled) return;
            _isAdvancedCycleTimingEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 0 - 默认
    /// 1 - 强制自然时间
    /// 2 - 强制课程时间
    /// </summary>
    public int NatureTimeUseMode
    {
        get => _natureTimeUseMode;
        set
        {
            if (value == _natureTimeUseMode) return;
            _natureTimeUseMode = value;
            OnPropertyChanged();
        }
    }

    public DayOfWeek WeekCountdownStartDay
    {
        get => _weekCountdownStartDay;
        set
        {
            if (value == _weekCountdownStartDay) return;
            _weekCountdownStartDay = value;
            OnPropertyChanged();
        }
    }

    public bool IsCustomWeekCountdownStartDayEnabled
    {
        get => _isCustomWeekCountdownStartDayEnabled;
        set
        {
            if (value == _isCustomWeekCountdownStartDayEnabled) return;
            _isCustomWeekCountdownStartDayEnabled = value;
            OnPropertyChanged();
        }
    }
}
