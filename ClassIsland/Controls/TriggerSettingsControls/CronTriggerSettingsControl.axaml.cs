using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.Automation.Triggers;

namespace ClassIsland.Controls.TriggerSettingsControls;

/// <summary>
/// CronTriggerSettingsControl.axaml 的交互逻辑
/// </summary>
public partial class CronTriggerSettingsControl : TriggerSettingsControlBase<CronTriggerSettings>
{
    public CronTriggerSettingsControl()
    {
        InitializeComponent();
    }
}