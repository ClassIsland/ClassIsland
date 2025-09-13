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
}
