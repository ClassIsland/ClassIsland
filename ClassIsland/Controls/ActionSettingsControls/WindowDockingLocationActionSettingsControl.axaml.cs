using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.Actions;

namespace ClassIsland.Controls.ActionSettingsControls;

public partial class WindowDockingLocationActionSettingsControl : ActionSettingsControlBase<WindowDockingLocationActionSettings>
{
    public WindowDockingLocationActionSettingsControl()
    {
        InitializeComponent();
        DataContext = this;
    }
}
