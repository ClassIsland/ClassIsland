using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.NotificationProviderSettings;

/// <summary>
/// 日程提醒提供方设置
/// </summary>
public class ReminderNotificationProviderSettings : ObservableRecipient
{
    private bool _isEnabled = true;
    private int _checkIntervalSeconds = 30;

    /// <summary>
    /// 是否启用日程提醒
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (value == _isEnabled) return;
            _isEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 检查间隔（秒），范围 5~600
    /// </summary>
    public int CheckIntervalSeconds
    {
        get => _checkIntervalSeconds;
        set
        {
            var clamped = Math.Clamp(value, 5, 600);
            if (clamped == _checkIntervalSeconds) return;
            _checkIntervalSeconds = clamped;
            OnPropertyChanged();
        }
    }
}
