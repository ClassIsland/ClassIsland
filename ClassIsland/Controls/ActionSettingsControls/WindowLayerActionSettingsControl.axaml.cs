using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.ActionSettings;

namespace ClassIsland.Controls.ActionSettingsControls;

public partial class WindowLayerActionSettingsControl : ActionSettingsControlBase<WindowLayerActionSettings>
{
    public WindowLayerActionSettingsControl()
    {
        InitializeComponent();
        DataContext = this;
    }
}
