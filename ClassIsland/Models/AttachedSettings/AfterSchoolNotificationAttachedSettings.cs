using ClassIsland.Shared.Abstraction.Models;
using ClassIsland.Shared.Interfaces;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.AttachedSettings;

public class AfterSchoolNotificationAttachedSettings : ObservableRecipient, IAttachedSettings, 
    IAfterSchoolNotificationProviderSettingsBase
{
    private bool _isEnabled = true;
    private string _notificationMsg = "今天的课程已结束，请同学们有序离开。";
    private bool _isAttachSettingsEnabled = false;

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
}