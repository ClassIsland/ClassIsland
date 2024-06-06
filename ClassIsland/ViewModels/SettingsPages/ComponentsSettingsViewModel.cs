using ClassIsland.Core.Models.Components;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public class ComponentsSettingsViewModel : ObservableRecipient
{
    private ComponentSettings? _selectedComponentSettings;
    private string _createProfileName = "";

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

    public string CreateProfileName
    {
        get => _createProfileName;
        set
        {
            if (value == _createProfileName) return;
            _createProfileName = value;
            OnPropertyChanged();
        }
    }
}