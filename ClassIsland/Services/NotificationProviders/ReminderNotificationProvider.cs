using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.NotificationProviderSettings;

namespace ClassIsland.Services.NotificationProviders;

/// <summary>
/// 日程提醒提供方，负责日程（Reminder）倒计时类的提醒。
/// </summary>
[NotificationProviderInfo("9972E45D-4471-435D-8F94-B2C1C5825EE7", "日程提醒", "\ue8bd", "在设定的日程时间发出提醒。")]
public class ReminderNotificationProvider : NotificationProviderBase<ReminderNotificationProviderSettings>
{
}
