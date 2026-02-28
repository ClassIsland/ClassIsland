using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class RefreshingSettingsViewModel(
    SettingsService settingsService, 
    ITutorialService tutorialService,
    IRefreshingService refreshingService) : ObservableObject
{
    public SettingsService SettingsService { get; } = settingsService;
    public ITutorialService TutorialService { get; } = tutorialService;
    public IRefreshingService RefreshingService { get; } = refreshingService;
}