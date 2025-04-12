using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.NotificationProviderSettings;

public class ClassPreparingNotificationSettings : ObservableRecipient, IClassPreparingNotificationSettings
{
    private bool _isClassOnPreparingNotificationEnabled = true;
    private int _inDoorClassPreparingDeltaTime = 60;
    private int _outDoorClassPreparingDeltaTime = 600;
    private string _classOnPreparingText = "准备上课，请回到座位并保持安静，做好上课准备。";
    private string _outdoorClassOnPreparingText = "下节课程为户外课程，请合理规划时间，做好上课准备。";
    private bool _isSpeechEnabledOnClassPreparing = true;
    private bool _showTeacherNameWhenClassPreparing = false;
    private string _classOnPreparingMaskText = "即将上课";
    private string _outdoorClassOnPreparingMaskText = "即将上课";

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

    public int InDoorClassPreparingDeltaTime
    {
        get => _inDoorClassPreparingDeltaTime;
        set
        {
            if (value.Equals(_inDoorClassPreparingDeltaTime)) return;
            _inDoorClassPreparingDeltaTime = value;
            OnPropertyChanged();
        }
    }

    public int OutDoorClassPreparingDeltaTime
    {
        get => _outDoorClassPreparingDeltaTime;
        set
        {
            if (value.Equals(_outDoorClassPreparingDeltaTime)) return;
            _outDoorClassPreparingDeltaTime = value;
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

    public string ClassOnPreparingMaskText
    {
        get => _classOnPreparingMaskText;
        set
        {
            if (value == _classOnPreparingMaskText) return;
            _classOnPreparingMaskText = value;
            OnPropertyChanged();
        }
    }

    public string OutdoorClassOnPreparingMaskText
    {
        get => _outdoorClassOnPreparingMaskText;
        set
        {
            if (value == _outdoorClassOnPreparingMaskText) return;
            _outdoorClassOnPreparingMaskText = value;
            OnPropertyChanged();
        }
    }

    public string OutdoorClassOnPreparingText
    {
        get => _outdoorClassOnPreparingText;
        set
        {
            if (value == _outdoorClassOnPreparingText) return;
            _outdoorClassOnPreparingText = value;
            OnPropertyChanged();
        }
    }

    public bool IsSpeechEnabledOnClassPreparing
    {
        get => _isSpeechEnabledOnClassPreparing;
        set
        {
            if (value == _isSpeechEnabledOnClassPreparing) return;
            _isSpeechEnabledOnClassPreparing = value;
            OnPropertyChanged();
        }
    }

    public bool ShowTeacherNameWhenClassPreparing
    {
        get => _showTeacherNameWhenClassPreparing;
        set
        {
            if (value == _showTeacherNameWhenClassPreparing) return;
            _showTeacherNameWhenClassPreparing = value;
            OnPropertyChanged();
        }
    }
}
