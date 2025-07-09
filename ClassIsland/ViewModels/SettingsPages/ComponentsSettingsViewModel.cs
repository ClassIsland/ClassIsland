using System.Collections.Generic;
using System.Collections.ObjectModel;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Components;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class ComponentsSettingsViewModel(IComponentsService componentsService, SettingsService settingsService) : ObservableRecipient
{
    public IComponentsService ComponentsService { get; } = componentsService;
    public SettingsService SettingsService { get; } = settingsService;
    
    [ObservableProperty] private ComponentSettings? _selectedComponentSettings;
    [ObservableProperty] private string _createProfileName = "";
    [ObservableProperty] private int _settingsTabControlIndex = 0;
    [ObservableProperty] private bool _isComponentSettingsVisible = false;
    [ObservableProperty] private bool _isComponentAdvancedSettingsVisible = false;
    [ObservableProperty] private ObservableCollection<ComponentSettings> _selectedComponentContainerChildren = new();
    [ObservableProperty] private bool _isComponentChildrenViewOpen = false;
    [ObservableProperty] private bool _isSelectedInChildrenListBox = false;
    [ObservableProperty] private ComponentSettings? _selectedComponentSettingsMain;
    [ObservableProperty] private ComponentSettings? _selectedComponentSettingsChild;
    [ObservableProperty] private Stack<ComponentSettings> _childrenComponentSettingsNavigationStack = new();
    [ObservableProperty] private ComponentSettings? _selectedContainerComponent;
    [ObservableProperty] private bool _canChildrenNavigateBack = false;
    [ObservableProperty] private ComponentSettings? _selectedRootComponent;
    [ObservableProperty] private MainWindowLineSettings? _selectedMainWindowLineSettings;


}