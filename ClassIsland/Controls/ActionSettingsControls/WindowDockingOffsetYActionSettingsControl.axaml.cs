using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.Actions;

namespace ClassIsland.Controls.ActionSettingsControls;

public partial class WindowDockingOffsetYActionSettingsControl : ActionSettingsControlBase<WindowDockingOffsetYActionSettings>
{
    public WindowDockingOffsetYActionSettingsControl()
    {
        InitializeComponent();
        DataContext = this;
    }
}
