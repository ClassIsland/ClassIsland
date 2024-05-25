using ClassIsland.Core.Abstraction.Models;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Notification;

public class NotificationSettings : ObservableRecipient, INotificationSettings
{
    private bool _isNotificationEnabled = true;
    private bool _isSpeechEnabled = false;
    private bool _isNotificationEffectEnabled = true;
    private bool _isNotificationSoundEnabled = false;
    private string _notificationSoundPath = "";
    private bool _isSettingsEnabled = false;
    private bool _isNotificationTopmostEnabled = false;

    public bool IsNotificationEnabled
    {
        get => _isNotificationEnabled;
        set
        {
            if (value == _isNotificationEnabled) return;
            _isNotificationEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsSpeechEnabled
    {
        get => _isSpeechEnabled;
        set
        {
            if (value == _isSpeechEnabled) return;
            _isSpeechEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsNotificationEffectEnabled
    {
        get => _isNotificationEffectEnabled;
        set
        {
            if (value == _isNotificationEffectEnabled) return;
            _isNotificationEffectEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsNotificationSoundEnabled
    {
        get => _isNotificationSoundEnabled;
        set
        {
            if (value == _isNotificationSoundEnabled) return;
            _isNotificationSoundEnabled = value;
            OnPropertyChanged();
        }
    }

    public string NotificationSoundPath
    {
        get => _notificationSoundPath;
        set
        {
            if (value == _notificationSoundPath) return;
            _notificationSoundPath = value;
            OnPropertyChanged();
        }
    }

    public bool IsNotificationTopmostEnabled
    {
        get => _isNotificationTopmostEnabled;
        set
        {
            if (value == _isNotificationTopmostEnabled) return;
            _isNotificationTopmostEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsSettingsEnabled
    {
        get => _isSettingsEnabled;
        set
        {
            if (value == _isSettingsEnabled) return;
            _isSettingsEnabled = value;
            OnPropertyChanged();
        }
    }

    
}