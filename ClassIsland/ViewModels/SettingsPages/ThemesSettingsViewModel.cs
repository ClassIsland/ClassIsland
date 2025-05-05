using ClassIsland.Core.Models.Plugin;
using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;
using System.Windows.Documents;
using ClassIsland.Core.Models.XamlTheme;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class ThemesSettingsViewModel : ObservableObject
{
    [ObservableProperty] private ThemeInfo? _selectedThemeInfo;
    [ObservableProperty] private bool _isThemeOperationsPopupOpened = false;
    [ObservableProperty] private bool _isThemeMarketOperationsPopupOpened = false;
    [ObservableProperty] private ThemeIndexItem? _selectedThemeIndexInfo;
    [ObservableProperty] private int _themeCategoryIndex = 1;
    [ObservableProperty] private string _themeFilterText = "";
    [ObservableProperty] private SnackbarMessageQueue _messageQueue = new();
    [ObservableProperty] private bool _isDragEntering = false;
}