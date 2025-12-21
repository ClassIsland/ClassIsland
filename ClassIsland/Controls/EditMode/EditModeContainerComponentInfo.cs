using System.Collections.Generic;
using System.Linq;
using ClassIsland.Core.Models.Components;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Controls.EditMode;

public partial class EditModeContainerComponentInfo(
    ComponentSettings settings,
    IList<ComponentSettings> componentStack) : ObservableObject
{
    [ObservableProperty] private ComponentSettings _settings = settings;
    [ObservableProperty] private IList<ComponentSettings> _componentStack = componentStack;
    [ObservableProperty] private double _x;
    [ObservableProperty] private double _y;

    public ComponentSettings? ParentComponent => ComponentStack.LastOrDefault();
}