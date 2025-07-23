using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class ErrorSettingsViewModel : ObservableRecipient
{
    [ObservableProperty] private bool _isError = false;
}