using System.Collections.Generic;
using System.Collections.ObjectModel;
using ClassIsland.Controls.EditMode;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Models.Components;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.EditMode;

public partial class EditModeViewModel(
    MainWindow mainWindow,
    IComponentsService componentsService,
    SettingsService settingsService,
    IUriNavigationService uriNavigationService,
    ITutorialService tutorialService) : ObservableObject
{
    public MainWindow MainWindow { get; } = mainWindow;
    public IComponentsService ComponentsService { get; } = componentsService;
    public SettingsService SettingsService { get; } = settingsService;
    public IUriNavigationService UriNavigationService { get; } = uriNavigationService;
    public ITutorialService TutorialService { get; } = tutorialService;

    public MainViewModel MainViewModel => MainWindow.ViewModel;

    [ObservableProperty] private object? _mainDrawerContent;
    [ObservableProperty] private object? _mainDrawerTitle;
    [ObservableProperty] private VerticalDrawerOpenState _mainDrawerState;
    [ObservableProperty] private bool _isDrawerTempCollapsed;
    [ObservableProperty] private IReadOnlyList<ComponentInfo> _componentInfos = [];
    [ObservableProperty] private int _componentSettingsTabIndex = 0;
    [ObservableProperty] private string _filterText = "";
    [ObservableProperty] private string _createProfileName = "";
    [ObservableProperty] private IList<ComponentSettings>? _targetComponentsList;
    [ObservableProperty] private ComponentInfo? _selectedComponentInfo;
    
    [ObservableProperty] private object? _secondaryDrawerContent;
    [ObservableProperty] private object? _secondaryDrawerTitle;
    [ObservableProperty] private VerticalDrawerOpenState _secondaryDrawerState;
    [ObservableProperty] private MainWindowLineSettings? _selectedMainWindowLineSettings;

    [ObservableProperty]
    private Dictionary<ComponentSettings, EditModeContainerComponentInfo> _containerComponentCache = [];
}