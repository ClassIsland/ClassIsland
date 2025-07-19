using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.Actions;

namespace ClassIsland.Controls.ActionSettingsControls;

public partial class AppRestartActionSettingsControl : ActionSettingsControlBase<AppRestartActionSettings>
{
    public AppRestartActionSettingsControl()
    {
        InitializeComponent();
        DataContext = this;
    }
}
