using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Notification;

public class NotificationProviderSettings : ObservableRecipient
{
    private bool _isNotificationEnabled = true;
    private bool _isSpeechEnabled = true;
    private bool _isNotificationEffectEnabled = true;

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
}