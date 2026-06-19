using System;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.NotificationProviderSettings;

namespace ClassIsland.Controls.NotificationProviders;

/// <summary>
/// 日程提醒提供方设置控件
/// </summary>
public partial class ReminderNotificationProviderSettingsControl : NotificationProviderControlBase<ReminderNotificationProviderSettings>
{
    public ReminderNotificationProviderSettingsControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 供 ComboBox 绑定使用的枚举值列表
    /// </summary>
    public Array CheckModeValues => Enum.GetValues(typeof(ReminderCheckMode));
}
