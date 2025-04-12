using ClassIsland.Shared.Interfaces;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.AttachedSettings;

public class ClassPreparingNotificationAttachedSettings : ObservableRecipient, IAttachedSettings, IClassPreparingNotificationSettings
{
    private bool _isClassOnPreparingNotificationEnabled = true;
    private int _classPreparingDeltaTime = 60;
    private string _classOnPreparingText = "准备上课，请回到座位并保持安静，做好上课准备。";
    private bool _isAttachSettingsEnabled;
    private string _outdoorClassOnPreparingText = "下节课程为户外课程，请合理规划时间，做好上课准备。";
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
