using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.ActionSettings;

namespace ClassIsland.Controls.ActionSettingsControls;

public partial class ThemeActionSettingsControl : ActionSettingsControlBase<ThemeActionSettings>
{
    public ThemeActionSettingsControl()
    {
        InitializeComponent();
        DataContext = this;
    }
}
