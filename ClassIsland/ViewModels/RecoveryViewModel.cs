using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class RecoveryViewModel : ObservableObject
{
    [ObservableProperty] private bool _canGoBack;
}