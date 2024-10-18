using ClassIsland.Shared.Abstraction.Models;
using ClassIsland.Shared.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.AttachedSettings;

public class LessonControlAttachedSettings : ObservableRecipient, IAttachedSettings, ILessonControlSettings
{
    private bool _isAttachSettingsEnabled = false;
    private bool _showExtraInfoOnTimePoint = true;
    private int _extraInfoType = 0;
    private bool _isCountdownEnabled = true;
    private int _countdownSeconds = 60;
    private int _extraInfo4ShowSecondsSeconds = 0;
    private double _scheduleSpacing = 1;
    private bool _showCurrentLessonOnlyOnClass = false;

    /// <inheritdoc />
    public bool IsAttachSettingsEnabled
    {
        get => _isAttachSettingsEnabled;
        set
        {
            if (value == _isAttachSettingsEnabled) return;
            _isAttachSettingsEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public double ScheduleSpacing
    {
        get => _scheduleSpacing;
        set
        {
            if (value.Equals(_scheduleSpacing)) ;
            _scheduleSpacing = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc />
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
}