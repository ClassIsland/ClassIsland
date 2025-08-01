using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.ActionSettings;

namespace ClassIsland.Controls.ActionSettingsControls;

public partial class WindowDockingLocationActionSettingsControl : ActionSettingsControlBase<WindowDockingLocationActionSettings>
{
    public WindowDockingLocationActionSettingsControl()
    {
        InitializeComponent();
        DataContext = this;
    }
}
