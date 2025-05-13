using ClassIsland.Shared.Interfaces;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.AttachedSettings;

public class ClassNotificationAttachedSettings : ObservableRecipient, IAttachedSettings, IClassNotificationSettings
{
    private bool _isClassOnNotificationEnabled = true;
    private bool _isClassOnPreparingNotificationEnabled = true;
    private bool _isClassOffNotificationEnabled = true;
    private int _classPreparingDeltaTime = 60;
    private string _classOnPreparingText = "准备上课，请回到座位并保持安静，做好上课准备。";
    private bool _isAttachSettingsEnabled;
    private string _outdoorClassOnPreparingText = "下节课程为户外课程，请合理规划时间，做好上课准备。";
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
