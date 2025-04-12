using ClassIsland.Shared.Interfaces;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.AttachedSettings;

public class ClassOnNotificationAttachedSettings : ObservableRecipient, IAttachedSettings, IClassOnNotificationSettings
{
    private bool _isClassOnNotificationEnabled = true;
    private bool _isAttachSettingsEnabled;
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