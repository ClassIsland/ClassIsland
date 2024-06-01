using ClassIsland.Shared.Interfaces;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.AttachedSettings;

public class ClassNotificationAttachedSettings : ObservableRecipient, IAttachedSettings
{
    private bool _isClassOnNotificationEnabled = true;
    private bool _isClassOnPreparingNotificationEnabled = true;
    private bool _isClassOffNotificationEnabled = true;
    private int _classPreparingDeltaTime = 60;
    private string _classOnPreparingText = "准备上课，请回到座位并保持安静，做好上课准备。";
    private bool _isAttachSettingsEnabled;

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

    public bool IsClassOnPreparingNotificationEnabled
    {
        get => _isClassOnPreparingNotificationEnabled;
        set
        {
            if (value == _isClassOnPreparingNotificationEnabled) return;
            _isClassOnPreparingNotificationEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsClassOffNotificationEnabled
    {
        get => _isClassOffNotificationEnabled;
        set
        {
            if (value == _isClassOffNotificationEnabled) return;
            _isClassOffNotificationEnabled = value;
            OnPropertyChanged();
        }
    }

    public int ClassPreparingDeltaTime
    {
        get => _classPreparingDeltaTime;
        set
        {
            if (value == _classPreparingDeltaTime) return;
            _classPreparingDeltaTime = value;
            OnPropertyChanged();
        }
    }

    public string ClassOnPreparingText
    {
        get => _classOnPreparingText;
        set
        {
            if (value == _classOnPreparingText) return;
            _classOnPreparingText = value;
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