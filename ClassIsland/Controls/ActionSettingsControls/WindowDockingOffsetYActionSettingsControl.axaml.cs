using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.ActionSettings;

namespace ClassIsland.Controls.ActionSettingsControls;

public partial class WindowDockingOffsetYActionSettingsControl : ActionSettingsControlBase<WindowDockingOffsetYActionSettings>
{
    public WindowDockingOffsetYActionSettingsControl()
    {
        InitializeComponent();
        DataContext = this;
    }
}
