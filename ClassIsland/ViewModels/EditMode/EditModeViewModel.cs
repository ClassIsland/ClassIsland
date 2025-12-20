using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.EditMode;

public partial class EditModeViewModel(MainWindow mainWindow, IComponentsService componentsService, SettingsService settingsService) : ObservableObject
{
    public MainWindow MainWindow { get; } = mainWindow;
    public IComponentsService ComponentsService { get; } = componentsService;
    public SettingsService SettingsService { get; } = settingsService;

    [ObservableProperty] private object? _mainDrawerContent;
    [ObservableProperty] private object? _mainDrawerTitle;
    [ObservableProperty] private VerticalDrawerOpenState _mainDrawerState;
    [ObservableProperty] private bool _isDrawerTempCollapsed;
}