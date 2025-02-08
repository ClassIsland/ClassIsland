using ClassIsland.Core.Abstractions.Services;

namespace ClassIsland.Controls.ActionSettingsControls;

public partial class CurrentComponentConfigActionSettingsControl
{
    public CurrentComponentConfigActionSettingsControl(IComponentsService componentsService)
    {
        ComponentsService = componentsService;
        InitializeComponent();
        DataContext = this;
    }

    public IComponentsService ComponentsService { get; }
}