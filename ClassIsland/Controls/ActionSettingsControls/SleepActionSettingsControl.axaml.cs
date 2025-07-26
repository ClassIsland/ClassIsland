using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.Actions;

namespace ClassIsland.Controls.ActionSettingsControls;

public partial class SleepActionSettingsControl : ActionSettingsControlBase<SleepActionSettings>
{
    public SleepActionSettingsControl()
    {
        InitializeComponent();
        DataContext = this;
    }
}
