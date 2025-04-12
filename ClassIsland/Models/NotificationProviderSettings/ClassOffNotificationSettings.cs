using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.NotificationProviderSettings;

public class ClassOffNotificationSettings : ObservableRecipient, IClassOffNotificationSettings
{
    private bool _isClassOffNotificationEnabled = true;
    private bool _isSpeechEnabledOnClassOff = true;
    private bool _showTeacherNameWhenClassOff = false;
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
    public bool IsSpeechEnabledOnClassOff
    {
        get => _isSpeechEnabledOnClassOff;
        set
        {
            if (value == _isSpeechEnabledOnClassOff) return;
            _isSpeechEnabledOnClassOff = value;
            OnPropertyChanged();
        }
    }
    public bool ShowTeacherNameWhenClassOff
    {
        get => _showTeacherNameWhenClassOff;
        set
        {
            if (value == _showTeacherNameWhenClassOff) return;
            _showTeacherNameWhenClassOff = value;
            OnPropertyChanged();
        }
    }
}