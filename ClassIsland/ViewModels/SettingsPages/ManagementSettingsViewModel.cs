using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class ManagementSettingsViewModel : ObservableObject
{
    [ObservableProperty] private Geometry? _cuidQrCodePath;
    [ObservableProperty] private bool _isRemoteAssistEnabled;
    [ObservableProperty] private string _remoteAssistPin = "";
    [ObservableProperty] private bool _isPinVisible;
}
