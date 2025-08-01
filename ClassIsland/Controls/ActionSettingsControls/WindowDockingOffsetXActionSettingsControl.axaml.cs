using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.ActionSettings;

namespace ClassIsland.Controls.ActionSettingsControls;

public partial class WindowDockingOffsetXActionSettingsControl : ActionSettingsControlBase<WindowDockingOffsetXActionSettings>
{
    public WindowDockingOffsetXActionSettingsControl()
    {
        InitializeComponent();
        DataContext = this;
    }
}
