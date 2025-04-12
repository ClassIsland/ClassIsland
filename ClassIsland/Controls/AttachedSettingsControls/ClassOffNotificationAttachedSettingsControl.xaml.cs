using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls.AttachedSettingsControls;

/// <summary>
/// ClassNotificationAttachedSettingsControl.xaml 的交互逻辑
/// </summary>
[AttachedSettingsUsage(AttachedSettingsTargets.ClassPlan | AttachedSettingsTargets.TimeLayout |
                       AttachedSettingsTargets.Lesson | AttachedSettingsTargets.Subject |
                       AttachedSettingsTargets.TimePoint)]
[AttachedSettingsControlInfo("D36D0B6B-DBEC-23DD-EF2B-F313C419A16E", "下课提醒设置", PackIconKind.BellNotificationOutline)]
public partial class ClassOffNotificationAttachedSettingsControl
{
    public ClassOffNotificationAttachedSettingsControl()
    {
        InitializeComponent();
    }
}