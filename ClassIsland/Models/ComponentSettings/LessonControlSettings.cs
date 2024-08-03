using ClassIsland.Shared.Abstraction.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.ComponentSettings;

public class LessonControlSettings : ObservableRecipient, ILessonControlSettings
{
    private bool _showExtraInfoOnTimePoint = true;
    private int _extraInfoType = 0;
    private bool _isCountdownEnabled = true;
    private int _countdownSeconds = 60;
    private bool _showCurrentLessonOnlyOnClass = false;
    private bool _showPlaceholderOnEmptyClassPlan = true;
    private string _placeholderText = "今天没有课程。";

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

    public string PlaceholderText
    {
        get => _placeholderText;
        set
        {
            if (value == _placeholderText) return;
            _placeholderText = value;
            OnPropertyChanged();
        }
    }
}