using ClassIsland.Core.Models.Components;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public class ComponentsSettingsViewModel : ObservableRecipient
{
    private ComponentSettings? _selectedComponentSettings;
    private string _createProfileName = "";
    private int _settingsTabControlIndex = 0;
    private bool _isComponentSettingsVisible = false;
    private bool _isComponentAdvancedSettingsVisible = false;

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

    public int SettingsTabControlIndex
    {
        get => _settingsTabControlIndex;
        set
        {
            if (value == _settingsTabControlIndex) return;
            _settingsTabControlIndex = value;
            OnPropertyChanged();
        }
    }

    public bool IsComponentSettingsVisible
    {
        get => _isComponentSettingsVisible;
        set
        {
            if (value == _isComponentSettingsVisible) return;
            _isComponentSettingsVisible = value;
            OnPropertyChanged();
        }
    }

    public bool IsComponentAdvancedSettingsVisible
    {
        get => _isComponentAdvancedSettingsVisible;
        set
        {
            if (value == _isComponentAdvancedSettingsVisible) return;
            _isComponentAdvancedSettingsVisible = value;
            OnPropertyChanged();
        }
    }
}