using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class InterfaceSettingsViewModel(SettingsService settingsService) : ObservableRecipient
{
    [ObservableProperty] private bool _isSelectingBackgroundImage;
    [ObservableProperty] private string _backgroundImageError = "";

    public SettingsService SettingsService { get; } = settingsService;
}
