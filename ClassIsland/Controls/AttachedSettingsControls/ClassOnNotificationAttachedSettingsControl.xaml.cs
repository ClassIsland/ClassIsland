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
[AttachedSettingsControlInfo("C308812E-3C3A-6E75-99A1-E6FC0D41B04A", "上课提醒设置", PackIconKind.BellNotificationOutline)]
public partial class ClassOnNotificationAttachedSettingsControl
{
    public ClassOnNotificationAttachedSettingsControl()
    {
        InitializeComponent();
    }
}