using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.NotificationProviderSettings;

/// <summary>
/// 日程提醒检查模式
/// </summary>
public enum ReminderCheckMode
{
    /// <summary>
    /// 窗口式检查：按配置间隔扫描时间窗口，使用 GetTriggerCandidatesInWindow
    /// </summary>
    WindowBased = 0,

    /// <summary>
    /// 分钟精度：固定 60 秒检查一次，使用 GetNextOccurrence 精确匹配当前分钟内的触发点
    /// </summary>
    Precise = 1,
}

/// <summary>
/// 日程提醒提供方设置
/// </summary>
public class ReminderNotificationProviderSettings : ObservableRecipient
{
    private bool _isEnabled = true;
    private int _checkIntervalSeconds = 30;
    private ReminderCheckMode _checkMode = ReminderCheckMode.Precise;

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

    /// <summary>
    /// 检查模式：WindowBased（窗口扫描）或 Precise（精确触发）
    /// </summary>
    public ReminderCheckMode CheckMode
    {
        get => _checkMode;
        set
        {
            if (value == _checkMode) return;
            _checkMode = value;
            OnPropertyChanged();
        }
    }
}
