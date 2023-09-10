using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.AttachedSettings;

public class AfterSchoolNotificationAttachedSettings : ObservableRecipient
{
    private bool _isStandaloneSettingsEnabled = false;
    private bool _isEnabled = true;
    private string _notificationMsg = "今天的课程已结束，请同学们有序离开。";

    public bool IsStandaloneSettingsEnabled
    {
        get => _isStandaloneSettingsEnabled;
        set
        {
            if (value == _isStandaloneSettingsEnabled) return;
            _isStandaloneSettingsEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (value == _isEnabled) return;
            _isEnabled = value;
            OnPropertyChanged();
        }
    }

    public string NotificationMsg
    {
        get => _notificationMsg;
        set
        {
            if (value == _notificationMsg) return;
            _notificationMsg = value;
            OnPropertyChanged();
        }
    }
}