using ClassIsland.Core.Abstraction.Models;
using ClassIsland.Core.Interfaces;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.AttachedSettings;

public class LessonControlAttachedSettings : ObservableRecipient, IAttachedSettings, ILessonControlSettings
{
    private bool _isAttachSettingsEnabled = false;
    private bool _showExtraInfoOnTimePoint = true;
    private int _extraInfoType = 0;
    private bool _isCountdownEnabled = true;
    private int _countdownSeconds = 60;

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
}