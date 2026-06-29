using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public class TrayMenuSettingsViewModel(SettingsService settingsService) : ObservableRecipient
{
    public SettingsService SettingsService { get; } = settingsService;
}
