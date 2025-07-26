using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.Actions;

namespace ClassIsland.Controls.ActionSettingsControls;

public partial class RunActionSettingsControl : ActionSettingsControlBase<RunActionSettings>
{
    public RunActionSettingsControl()
    {
        InitializeComponent();
        DataContext = this;
    }
}
