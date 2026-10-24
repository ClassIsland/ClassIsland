using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Icons;
using ClassIsland.Models.AttachedSettings;

namespace ClassIsland.Controls.AttachedSettingsControls;

[AttachedSettingsUsage(AttachedSettingsTargets.ClassPlan | AttachedSettingsTargets.TimeLayout |
                       AttachedSettingsTargets.Lesson | AttachedSettingsTargets.Subject |
                       AttachedSettingsTargets.TimePoint)]
[AttachedSettingsControlInfo(RealtimeNotificationAttachedSettings.SettingsId, "实时通知设置", FluentIcons.CardUiRegular)]
public partial class RealtimeNotificationAttachedSettingsControl : AttachedSettingsControlBase<RealtimeNotificationAttachedSettings>
{
    public RealtimeNotificationAttachedSettingsControl()
    {
        InitializeComponent();
    }
}
