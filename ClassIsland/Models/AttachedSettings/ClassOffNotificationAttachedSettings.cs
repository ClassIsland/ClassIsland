using ClassIsland.Shared.Interfaces;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.AttachedSettings;

public class ClassOffNotificationAttachedSettings : ObservableRecipient, IAttachedSettings, IClassOffNotificationSettings
{
    private bool _isClassOffNotificationEnabled = true;
    private bool _isAttachSettingsEnabled;
    private string _classOffMaskText = "课间休息";
    private string _classOffOverlayText = "";


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

    public string ClassOffMaskText
    {
        get => _classOffMaskText;
        set
        {
            if (value == _classOffMaskText) return;
            _classOffMaskText = value;
            OnPropertyChanged();
        }
    }

    public string ClassOffOverlayText
    {
        get => _classOffOverlayText;
        set
        {
            if (value == _classOffOverlayText) return;
            _classOffOverlayText = value;
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