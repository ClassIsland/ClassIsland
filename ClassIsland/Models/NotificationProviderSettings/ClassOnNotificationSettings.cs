using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.NotificationProviderSettings;

public class ClassOnNotificationSettings : ObservableRecipient, IClassOnNotificationSettings
{
    private bool _isClassOnNotificationEnabled = true;
    private bool _isSpeechEnabledOnClassOn = true;
    private string _classOnMaskText = "上课";

    public bool IsClassOnNotificationEnabled
    {
        get => _isClassOnNotificationEnabled;
        set
        {
            if (value == _isClassOnNotificationEnabled) return;
            _isClassOnNotificationEnabled = value;
            OnPropertyChanged();
        }
    }
    public string ClassOnMaskText
    {
        get => _classOnMaskText;
        set
        {
            if (value == _classOnMaskText) return;
            _classOnMaskText = value;
            OnPropertyChanged();
        }
    }
    public bool IsSpeechEnabledOnClassOn
    {
        get => _isSpeechEnabledOnClassOn;
        set
        {
            if (value == _isSpeechEnabledOnClassOn) return;
            _isSpeechEnabledOnClassOn = value;
            OnPropertyChanged();
        }
    }
}