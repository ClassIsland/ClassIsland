using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Notification;

public class NotificationSettings : ObservableRecipient
{
    private bool _isNotificationEnabled = true;
    private bool _isSpeechEnabled = true;
    private bool _isNotificationEffectEnabled = true;
    private bool _isNotificationSoundEnabled = true;
    private string _notificationSoundPath = "";

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
}