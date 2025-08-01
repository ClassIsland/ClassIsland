using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.ActionSettings;
namespace ClassIsland.Controls.ActionSettingsControls;

public partial class AppRestartActionSettingsControl : ActionSettingsControlBase<AppRestartActionSettings>
{
    public AppRestartActionSettingsControl()
    {
        InitializeComponent();
        DataContext = this;
    }
}
