using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.Automation.Triggers;

namespace ClassIsland.Controls.ActionSettingsControls;

/// <summary>
/// BroadcastSignalActionSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class BroadcastSignalActionSettingsControl : ActionSettingsControlBase<SignalTriggerSettings>
{
    public BroadcastSignalActionSettingsControl()
    {
        InitializeComponent();
    }
}
