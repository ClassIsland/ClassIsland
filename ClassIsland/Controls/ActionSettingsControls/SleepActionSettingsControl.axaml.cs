using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.ActionSettings;

namespace ClassIsland.Controls.ActionSettingsControls;

public partial class SleepActionSettingsControl : ActionSettingsControlBase<SleepActionSettings>
{
    public SleepActionSettingsControl()
    {
        InitializeComponent();
        DataContext = this;
    }
}
