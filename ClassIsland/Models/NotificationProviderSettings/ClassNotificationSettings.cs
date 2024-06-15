using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.NotificationProviderSettings;

public class ClassNotificationSettings : ObservableRecipient
{
    private bool _isClassOnNotificationEnabled = true;
    private bool _isClassOnPreparingNotificationEnabled = true;
    private bool _isClassOffNotificationEnabled = true;
    private int _inDoorClassPreparingDeltaTime = 60;
    private int _outDoorClassPreparingDeltaTime = 600;
    private string _classOnPreparingText = "准备上课，请回到座位并保持安静，做好上课准备。";
    private bool _isSpeechEnabledOnClassPreparing = true;
    private bool _isSpeechEnabledOnClassOn = true;
    private bool _isSpeechEnabledOnClassOff = true;
    private bool _showTeacherName = false;

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