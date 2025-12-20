using System.Collections.Generic;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.EditMode;

public partial class EditModeViewModel(
    MainWindow mainWindow,
    IComponentsService componentsService,
    SettingsService settingsService,
    IUriNavigationService uriNavigationService) : ObservableObject
{
    public MainWindow MainWindow { get; } = mainWindow;
    public IComponentsService ComponentsService { get; } = componentsService;
    public SettingsService SettingsService { get; } = settingsService;
    public IUriNavigationService UriNavigationService { get; } = uriNavigationService;

    [ObservableProperty] private object? _mainDrawerContent;
    [ObservableProperty] private object? _mainDrawerTitle;
    [ObservableProperty] private VerticalDrawerOpenState _mainDrawerState;
    [ObservableProperty] private bool _isDrawerTempCollapsed;
    [ObservableProperty] private IReadOnlyList<ComponentInfo> _componentInfos = [];
}