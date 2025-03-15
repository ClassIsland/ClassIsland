using ClassIsland.Shared.Abstraction.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.ComponentSettings;

public class LessonControlSettings : ObservableRecipient, ILessonControlSettings
{
    private bool _showExtraInfoOnTimePoint = true;
    private int _extraInfoType = 0;
    private bool _isCountdownEnabled = true;
    private int _countdownSeconds = 60;
    private int _extraInfo4ShowSecondsSeconds = 0;
    private double _scheduleSpacing = 1;
    private bool _showCurrentLessonOnlyOnClass = false;
    private bool _hideFinishedClass = false;
    private bool _showPlaceholderOnEmptyClassPlan = true;
    private string _placeholderTextNoClass = "今天没有课程。";
    private string _placeholderTextAllClassEnded = "今日课程已全部结束。";
    private bool _showTomorrowSchedules = false;
    private int _tomorrowScheduleShowMode = 1;
    private bool _highlightChangedClass = false;
    private bool _isNonExactCountdownEnabled = false;

    public bool ShowExtraInfoOnTimePoint
    {
        get => _showExtraInfoOnTimePoint;
        set
        {
            if (value == _showExtraInfoOnTimePoint) return;
            _showExtraInfoOnTimePoint = value;
            OnPropertyChanged();
        }
    }

    public int ExtraInfoType
    {
        get => _extraInfoType;
        set
        {
            if (value == _extraInfoType) return;
            _extraInfoType = value;
            OnPropertyChanged();
        }
    }

    public bool IsCountdownEnabled
    {
        get => _isCountdownEnabled;
        set
        {
            if (value == _isCountdownEnabled) return;
            _isCountdownEnabled = value;
            OnPropertyChanged();
        }
    }

    public int CountdownSeconds
    {
        get => _countdownSeconds;
        set
        {
            if (value == _countdownSeconds) return;
            _countdownSeconds = value;
            OnPropertyChanged();
        }
    }

    public int ExtraInfo4ShowSecondsSeconds
    {
        get => _extraInfo4ShowSecondsSeconds;
        set
        {
            if (value == _extraInfo4ShowSecondsSeconds) return;
            _extraInfo4ShowSecondsSeconds = value;
            OnPropertyChanged();
        }
    }

    public double ScheduleSpacing
    {
        get => _scheduleSpacing;
        set
        {
            if (value.Equals(_scheduleSpacing)) return;
            _scheduleSpacing = value;
            OnPropertyChanged();
        }
    }

    public bool ShowCurrentLessonOnlyOnClass
    {
        get => _showCurrentLessonOnlyOnClass;
        set
        {
            if (value == _showCurrentLessonOnlyOnClass) return;
            _showCurrentLessonOnlyOnClass = value;
            OnPropertyChanged();
        }
    }

    public bool IsNonExactCountdownEnabled
    {
        get => _isNonExactCountdownEnabled;
        set
        {
            if (value == _isNonExactCountdownEnabled) return;
            _isNonExactCountdownEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool ShowPlaceholderOnEmptyClassPlan
    {
        get => _showPlaceholderOnEmptyClassPlan;
        set
        {
            if (value == _showPlaceholderOnEmptyClassPlan) return;
            _showPlaceholderOnEmptyClassPlan = value;
            OnPropertyChanged();
        }
    }

    public string PlaceholderTextNoClass
    {
        get => _placeholderTextNoClass;
        set
        {
            if (value == _placeholderTextNoClass) return;
            _placeholderTextNoClass = value;
            OnPropertyChanged();
        }
    }

    public string PlaceholderTextAllClassEnded
    {
        get => _placeholderTextAllClassEnded;
        set
        {
            if (value == _placeholderTextAllClassEnded) return;
            _placeholderTextAllClassEnded = value;
            OnPropertyChanged();
        }
    }

    public int TomorrowScheduleShowMode
    {
        get => _tomorrowScheduleShowMode;
        set
        {
            if (value == _tomorrowScheduleShowMode) return;
            _tomorrowScheduleShowMode = value;
            OnPropertyChanged();
        }
    }

    public bool HighlightChangedClass
    {
        get => _highlightChangedClass;
        set
        {
            if (value == _highlightChangedClass) return;
            _highlightChangedClass = value;
            OnPropertyChanged();
        }
    }

    public bool HideFinishedClass
    {
        get => _hideFinishedClass;
        set
        {
            if (value == _hideFinishedClass) return;
            _hideFinishedClass = value;
            OnPropertyChanged();
        }
    }
}