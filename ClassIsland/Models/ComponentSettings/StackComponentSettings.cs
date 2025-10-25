using System.Collections.ObjectModel;
using ClassIsland.Core.Abstractions.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.ComponentSettings;

public partial class StackComponentSettings : ObservableObject, IComponentContainerSettings
{
    [ObservableProperty]
    private ObservableCollection<Core.Models.Components.ComponentSettings> _children = [];
}