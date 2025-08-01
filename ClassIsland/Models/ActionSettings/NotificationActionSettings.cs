using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.ActionSettings;

public class NotificationActionSettings : ObservableRecipient
{
    private string _content = "";
    private string _mask = "";
    private bool _isContentSpeechEnabled = true;
    private bool _isMaskSpeechEnabled = true;
    private bool _isSoundEffectEnabled = true;
    private bool _isTopmostEnabled = true;
    private string _customSoundEffectPath = "";
    private double _maskDurationSeconds = 5.0;
    private double _contentDurationSeconds = 10.0;
    private bool _isEffectEnabled = true;
    private bool _isAdvancedSettingsEnabled = false;

    public string Content
    {
        get => _content;
        set
        {
            if (value == _content) return;
            _content = value;
            OnPropertyChanged();
        }
    }

    public string Mask
    {
        get => _mask;
        set
        {
            if (value == _mask) return;
            _mask = value;
            OnPropertyChanged();
        }
    }

    public bool IsContentSpeechEnabled
    {
        get => _isContentSpeechEnabled;
        set
        {
            if (value == _isContentSpeechEnabled) return;
            _isContentSpeechEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsMaskSpeechEnabled
    {
        get => _isMaskSpeechEnabled;
        set
        {
            if (value == _isMaskSpeechEnabled) return;
            _isMaskSpeechEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsAdvancedSettingsEnabled
    {
        get => _isAdvancedSettingsEnabled;
        set
        {
            if (value == _isAdvancedSettingsEnabled) return;
            _isAdvancedSettingsEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsSoundEffectEnabled
    {
        get => _isSoundEffectEnabled;
        set
        {
            if (value == _isSoundEffectEnabled) return;
            _isSoundEffectEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsTopmostEnabled
    {
        get => _isTopmostEnabled;
        set
        {
            if (value == _isTopmostEnabled) return;
            _isTopmostEnabled = value;
            OnPropertyChanged();
        }
    }

    public string CustomSoundEffectPath
    {
        get => _customSoundEffectPath;
        set
        {
            if (value == _customSoundEffectPath) return;
            _customSoundEffectPath = value;
            OnPropertyChanged();
        }
    }

    public bool IsEffectEnabled
    {
        get => _isEffectEnabled;
        set
        {
            if (value == _isEffectEnabled) return;
            _isEffectEnabled = value;
            OnPropertyChanged();
        }
    }

    public double MaskDurationSeconds
    {
        get => _maskDurationSeconds;
        set
        {
            if (value.Equals(_maskDurationSeconds)) return;
            _maskDurationSeconds = value;
            OnPropertyChanged();
        }
    }

    public double ContentDurationSeconds
    {
        get => _contentDurationSeconds;
        set
        {
            if (value.Equals(_contentDurationSeconds)) return;
            _contentDurationSeconds = value;
            OnPropertyChanged();
        }
    }
}