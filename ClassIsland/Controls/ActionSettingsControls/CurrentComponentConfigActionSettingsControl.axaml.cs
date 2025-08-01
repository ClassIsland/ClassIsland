using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models.ActionSettings;

namespace ClassIsland.Controls.ActionSettingsControls;

public partial class CurrentComponentConfigActionSettingsControl : ActionSettingsControlBase<CurrentComponentConfigActionSettings>
{
    public CurrentComponentConfigActionSettingsControl(IComponentsService componentsService)
    {
        ComponentsService = componentsService;
        InitializeComponent();
        DataContext = this;
    }

    public IComponentsService ComponentsService { get; }
}
