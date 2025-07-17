using System;
using System.Collections.Generic;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.ComponentModels;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Services;
using ClassIsland.Views.SettingPages;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class PluginsSettingsPageViewModel : ObservableRecipient
{
    public IPluginService PluginService { get; }
    public IPluginMarketService PluginMarketService { get; }
    public SettingsService SettingsService { get; }
    public ILogger<PluginsSettingsPage> Logger { get; }
    
    [ObservableProperty] private PluginInfo? _selectedPluginInfo;
    [ObservableProperty] private string _readmeDocument = "";
    [ObservableProperty] private bool _isPluginOperationsPopupOpened = false;
    [ObservableProperty] private bool _isPluginMarketOperationsPopupOpened = false;
    [ObservableProperty] private PluginIndexInfo? _selectedPluginIndexInfo;
    [ObservableProperty] private int _pluginCategoryIndex = 1;
    [ObservableProperty] private string _pluginFilterText = "";
    [ObservableProperty] private bool _isLoadingDocument = false;
    [ObservableProperty] private bool _isDetailsShown = false;
    [ObservableProperty] private bool _isDragEntering = false;
    [ObservableProperty] private bool _pluginListBoxHasItems = false;
    [ObservableProperty] private IObservableList<KeyValuePair<string, PluginInfo>> _mergedPluginsFiltered = null!;
    [ObservableProperty] private SyncDictionaryList<string, string> _officialPluginMirrors = null!;

    public SyncDictionaryList<string, PluginInfo> MergedPlugins { get; }


    /// <inheritdoc/>
    public PluginsSettingsPageViewModel(IPluginService pluginService, IPluginMarketService pluginMarketService, SettingsService settingsService, ILogger<PluginsSettingsPage> logger)
    {
        PluginService = pluginService;
        PluginMarketService = pluginMarketService;
        SettingsService = settingsService;
        Logger = logger;

        MergedPlugins = new SyncDictionaryList<string, PluginInfo>(PluginMarketService.MergedPlugins, () => "");
        SettingsService.Settings
            .ObservableForProperty(x => x.OfficialIndexMirrors)
            .Subscribe(_ => UpdateOfficialPluginSources());
        
        UpdateMergedPlugins();
        UpdateOfficialPluginSources();
    }

    public void UpdateMergedPlugins()
    {
        MergedPluginsFiltered = MergedPlugins.List
            .ToObservableChangeSet()
            .Filter(PluginSourceFilter)
            .AsObservableList();
    }

    private void UpdateOfficialPluginSources()
    {
        OfficialPluginMirrors =
            new SyncDictionaryList<string, string>(SettingsService.Settings.OfficialIndexMirrors, () => "");
    }
    
    private bool PluginSourceFilter(KeyValuePair<string, PluginInfo> kvp)
    {
        var info = kvp.Value;
        if (!info.IsLocal && PluginCategoryIndex == 1)
        {
            return false;
        }
        if (!info.IsAvailableOnMarket && PluginCategoryIndex == 0)
        {
            return false;
        }
        
        var filter = PluginFilterText;
        if (string.IsNullOrWhiteSpace(filter))
            return true;
        return info.Manifest.Id.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
               info.Manifest.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
               info.Manifest.Description.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }
}
