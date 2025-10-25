using System;
using System.Collections.Generic;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.ComponentModels;
using ClassIsland.Core.Models.Plugin;
using CommunityToolkit.Mvvm.ComponentModel;
using ClassIsland.Core.Models.XamlTheme;
using ClassIsland.Services;
using DynamicData;
using DynamicData.Binding;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class ThemesSettingsViewModel : ObservableObject
{
    public IXamlThemeService XamlThemeService { get; }
    public IPluginMarketService PluginMarketService { get; }
    public SettingsService SettingsService { get; }
    
    [ObservableProperty] private ThemeInfo? _selectedThemeInfo;
    [ObservableProperty] private bool _isThemeOperationsPopupOpened = false;
    [ObservableProperty] private bool _isThemeMarketOperationsPopupOpened = false;
    [ObservableProperty] private ThemeIndexItem? _selectedThemeIndexInfo;
    [ObservableProperty] private int _themeCategoryIndex = 1;
    [ObservableProperty] private string _themeFilterText = "";
    [ObservableProperty] private bool _isDragEntering = false;
    
    [ObservableProperty] private IObservableList<KeyValuePair<string, ThemeInfo>> _mergedThemesFiltered = null!;

    public SyncDictionaryList<string, ThemeInfo> MergedThemes { get; set; } = null!;
    
    /// <inheritdoc/>
    public ThemesSettingsViewModel(IXamlThemeService xamlThemeService, IPluginMarketService pluginMarketService, SettingsService settingsService)
    {
        XamlThemeService = xamlThemeService;
        PluginMarketService = pluginMarketService;
        SettingsService = settingsService;
        
        UpdateMergedThemes();
    }
    
    public void UpdateMergedThemes()
    {
        MergedThemes = new SyncDictionaryList<string, ThemeInfo>(XamlThemeService.MergedThemes, () => "");
        MergedThemesFiltered = MergedThemes.List
            .ToObservableChangeSet()
            .Filter(ThemeSourceFilter)
            .AsObservableList();
    }
    
    private bool ThemeSourceFilter(KeyValuePair<string, ThemeInfo> kvp)
    {
        var info = kvp.Value;
        if (!info.IsLocal && ThemeCategoryIndex == 1)
        {
            return false;
        }
        if (!info.IsAvailableOnMarket && ThemeCategoryIndex == 0)
        {
            return false;
        }

        var filter = ThemeFilterText;
        if (string.IsNullOrWhiteSpace(filter))
            return true;
        return info.Manifest.Id.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                 info.Manifest.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                 info.Manifest.Description.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

}
