using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.Actions;

namespace ClassIsland.Controls.ActionSettingsControls;

public partial class WindowDockingOffsetXActionSettingsControl : ActionSettingsControlBase<WindowDockingOffsetXActionSettings>
{
    public WindowDockingOffsetXActionSettingsControl()
    {
        InitializeComponent();
        DataContext = this;
    }
}
