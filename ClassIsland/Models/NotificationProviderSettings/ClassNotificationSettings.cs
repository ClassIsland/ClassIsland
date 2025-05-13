using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.NotificationProviderSettings;

public class ClassNotificationSettings : ObservableRecipient, IClassNotificationSettings
{
    private bool _isClassOnNotificationEnabled = true;
    private bool _isClassOnPreparingNotificationEnabled = true;
    private bool _isClassOffNotificationEnabled = true;
    private int _inDoorClassPreparingDeltaTime = 60;
    private int _outDoorClassPreparingDeltaTime = 600;
    private string _classOnPreparingText = "准备上课，请回到座位并保持安静，做好上课准备。";
    private string _outdoorClassOnPreparingText = "下节课程为户外课程，请合理规划时间，做好上课准备。";
    private bool _isSpeechEnabledOnClassPreparing = true;
    private bool _isSpeechEnabledOnClassOn = true;
    private bool _isSpeechEnabledOnClassOff = true;
    private bool _showTeacherName = false;
    private string _classOnPreparingMaskText = "即将上课";
    private string _outdoorClassOnPreparingMaskText = "即将上课";
    private string _classOnMaskText = "上课";
    private string _classOffMaskText = "课间休息";
    private string _classOffOverlayText = "";

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

    public bool ShowTeacherName
    {
        get => _showTeacherName;
        set
        {
            if (value == _showTeacherName) return;
            _showTeacherName = value;
            OnPropertyChanged();
        }
    }
}
