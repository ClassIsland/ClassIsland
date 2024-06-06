using ClassIsland.Core.Models.Components;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public class ComponentsSettingsViewModel : ObservableRecipient
{
    private ComponentSettings? _selectedComponentSettings;

    public ComponentSettings? SelectedComponentSettings
    {
        get => _selectedComponentSettings;
        set
        {
            if (Equals(value, _selectedComponentSettings)) return;
            _selectedComponentSettings = value;
            OnPropertyChanged();
        }
    }
}