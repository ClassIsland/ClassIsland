using ClassIsland.Shared.Abstraction.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Shared.Models.Notification;

/// <summary>
/// 提醒设置
/// </summary>
public class NotificationSettings : ObservableRecipient, INotificationSettings
{
    private bool _isNotificationEnabled = true;
    private bool _isSpeechEnabled = false;
    private bool _isNotificationEffectEnabled = true;
    private bool _isNotificationSoundEnabled = false;
    private string _notificationSoundPath = "";
    private bool _isSettingsEnabled = false;
    private bool _isNotificationTopmostEnabled = false;

    /// <summary>
    /// 是否启用提醒
    /// </summary>
    public bool IsNotificationEnabled
    {
        get => _isNotificationEnabled;
        set
        {
            if (value == _isNotificationEnabled) return;
            _isNotificationEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否启用语音
    /// </summary>
    public bool IsSpeechEnabled
    {
        get => _isSpeechEnabled;
        set
        {
            if (value == _isSpeechEnabled) return;
            _isSpeechEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否启用提醒效果
    /// </summary>
    public bool IsNotificationEffectEnabled
    {
        get => _isNotificationEffectEnabled;
        set
        {
            if (value == _isNotificationEffectEnabled) return;
            _isNotificationEffectEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否启用提醒音效
    /// </summary>
    public bool IsNotificationSoundEnabled
    {
        get => _isNotificationSoundEnabled;
        set
        {
            if (value == _isNotificationSoundEnabled) return;
            _isNotificationSoundEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 提醒音效路径
    /// </summary>
    public string NotificationSoundPath
    {
        get => _notificationSoundPath;
        set
        {
            if (value == _notificationSoundPath) return;
            _notificationSoundPath = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否在提醒时置顶主界面
    /// </summary>
    public bool IsNotificationTopmostEnabled
    {
        get => _isNotificationTopmostEnabled;
        set
        {
            if (value == _isNotificationTopmostEnabled) return;
            _isNotificationTopmostEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否启用此提醒设置。如果为false，那么这里设置的提醒设置将不起作用。
    /// </summary>
    public bool IsSettingsEnabled
    {
        get => _isSettingsEnabled;
        set
        {
            if (value == _isSettingsEnabled) return;
            _isSettingsEnabled = value;
            OnPropertyChanged();
        }
    }

    
}