using System.Collections.ObjectModel;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Components;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Services;

public class ComponentsService : ObservableRecipient, IComponentsService
{
    private ObservableCollection<ComponentSettings> _currentComponents = new();

    public ObservableCollection<ComponentSettings> CurrentComponents
    {
        get => _currentComponents;
        set
        {
            if (Equals(value, _currentComponents)) return;
            _currentComponents = value;
            OnPropertyChanged();
        }
    }
}