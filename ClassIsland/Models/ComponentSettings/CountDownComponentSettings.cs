using System;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.ComponentSettings;

public class CountDownComponentSettings : ObservableRecipient
{
    private string _countDownName = "倒计时";
    private DateTime _overTime = DateTime.Now;
    private int _daysLeft = 0;
    private Color _fontColor = Color.FromRgb(255,0,0);
    private int _fontSize = 16;
    private bool _isCompactModeEnabled = false;
    private string _countDownConnector = "还有";
    private bool _isConnectorColorEmphasized = false;

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
}
