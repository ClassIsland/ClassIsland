using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class ManagementCredentialsSettingsViewModel : ObservableObject
{
    [ObservableProperty] private bool _isLocked = true;
}