using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.Actions;

namespace ClassIsland.Controls.ActionSettingsControls;

public partial class ThemeActionSettingsControl : ActionSettingsControlBase<ThemeActionSettings>
{
    public ThemeActionSettingsControl()
    {
        InitializeComponent();
        DataContext = this;
    }
}
