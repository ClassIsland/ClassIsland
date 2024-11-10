using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    private ObservableCollection<ComponentSettings> _selectedComponentContainerChildren = new();
    private bool _isComponentChildrenViewOpen = false;
    private bool _isSelectedInChildrenListBox = false;
    private ComponentSettings? _selectedComponentSettingsMain;
    private ComponentSettings? _selectedComponentSettingsChild;
    private Stack<ComponentSettings> _childrenComponentSettingsNavigationStack = new();
    private ComponentSettings? _selectedContainerComponent;
    private bool _canChildrenNavigateBack = false;
    private ComponentSettings? _selectedRootComponent;

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

    public ObservableCollection<ComponentSettings> SelectedComponentContainerChildren
    {
        get => _selectedComponentContainerChildren;
        set
        {
            if (Equals(value, _selectedComponentContainerChildren)) return;
            _selectedComponentContainerChildren = value;
            OnPropertyChanged();
        }
    }

    public bool IsComponentChildrenViewOpen
    {
        get => _isComponentChildrenViewOpen;
        set
        {
            if (value == _isComponentChildrenViewOpen) return;
            _isComponentChildrenViewOpen = value;
            OnPropertyChanged();
        }
    }

    public bool IsSelectedInChildrenListBox
    {
        get => _isSelectedInChildrenListBox;
        set
        {
            if (value == _isSelectedInChildrenListBox) return;
            _isSelectedInChildrenListBox = value;
            OnPropertyChanged();
        }
    }

    public ComponentSettings? SelectedComponentSettingsMain
    {
        get => _selectedComponentSettingsMain;
        set
        {
            if (Equals(value, _selectedComponentSettingsMain)) return;
            _selectedComponentSettingsMain = value;
            OnPropertyChanged();
        }
    }

    public ComponentSettings? SelectedComponentSettingsChild
    {
        get => _selectedComponentSettingsChild;
        set
        {
            if (Equals(value, _selectedComponentSettingsChild)) return;
            _selectedComponentSettingsChild = value;
            OnPropertyChanged();
        }
    }

    public Stack<ComponentSettings> ChildrenComponentSettingsNavigationStack
    {
        get => _childrenComponentSettingsNavigationStack;
        set
        {
            if (Equals(value, _childrenComponentSettingsNavigationStack)) return;
            _childrenComponentSettingsNavigationStack = value;
            OnPropertyChanged();
        }
    }

    public ComponentSettings? SelectedContainerComponent
    {
        get => _selectedContainerComponent;
        set
        {
            if (Equals(value, _selectedContainerComponent)) return;
            _selectedContainerComponent = value;
            OnPropertyChanged();
        }
    }

    public bool CanChildrenNavigateBack
    {
        get => _canChildrenNavigateBack;
        set
        {
            if (value == _canChildrenNavigateBack) return;
            _canChildrenNavigateBack = value;
            OnPropertyChanged();
        }
    }

    public ComponentSettings? SelectedRootComponent
    {
        get => _selectedRootComponent;
        set
        {
            if (Equals(value, _selectedRootComponent)) return;
            _selectedRootComponent = value;
            OnPropertyChanged();
        }
    }
}