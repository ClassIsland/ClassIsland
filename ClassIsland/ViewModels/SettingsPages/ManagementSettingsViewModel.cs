using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class ManagementSettingsViewModel : ObservableObject
{
    [ObservableProperty] private Geometry? _cuidQrCodePath;
}