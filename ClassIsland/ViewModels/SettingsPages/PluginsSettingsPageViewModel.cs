using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
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
    [ObservableProperty] private bool _isInstallingLocalPlugin = false;
    [ObservableProperty] private bool _isDetailsShown = false;
    [ObservableProperty] private bool _isDragEntering = false;
    [ObservableProperty] private bool _isDragInstallValid = false;
    [ObservableProperty] private int _dragInstallTotalCount = 0;
    [ObservableProperty] private int _dragInstallSupportedCount = 0;
    [ObservableProperty] private string _dragInstallHintText = "将插件拖入到此处，松手即可安装。";
    [ObservableProperty] private string _dragInstallSubHintText = "";
    [ObservableProperty] private bool _pluginListBoxHasItems = false;

    private ReadOnlyObservableCollection<KeyValuePair<string, PluginInfo>> _mergedPluginsFiltered = null!;
    public ReadOnlyObservableCollection<KeyValuePair<string, PluginInfo>> MergedPluginsFiltered => _mergedPluginsFiltered;

    private SyncDictionaryList<string, string> _officialPluginMirrors = null!;
    public SyncDictionaryList<string, string> OfficialPluginMirrors
    {
        get => _officialPluginMirrors;
        private set
        {
            if (ReferenceEquals(_officialPluginMirrors, value))
            {
                return;
            }

            _officialPluginMirrors?.Dispose();
            _officialPluginMirrors = value;
            OnPropertyChanged();
        }
    }

    private SyncDictionaryList<string, PluginInfo> _mergedPlugins = null!;
    public SyncDictionaryList<string, PluginInfo> MergedPlugins => _mergedPlugins;

    private IDisposable? _officialIndexMirrorsSubscription;
    private IDisposable? _mergedPluginsSubscription;
    private bool _isActivated;

    /// <inheritdoc/>
    public PluginsSettingsPageViewModel(IPluginService pluginService, IPluginMarketService pluginMarketService, SettingsService settingsService, ILogger<PluginsSettingsPage> logger)
    {
        PluginService = pluginService;
        PluginMarketService = pluginMarketService;
        SettingsService = settingsService;
        Logger = logger;

        Activate();
        UpdateOfficialPluginSources();
    }

    /// <summary>
    /// 订阅全局服务和集合，用于更新插件列表。页面加载时调用。
    /// </summary>
    public void Activate()
    {
        if (_isActivated)
        {
            return;
        }

        _isActivated = true;
        _mergedPlugins = new SyncDictionaryList<string, PluginInfo>(PluginMarketService.MergedPlugins, () => "");
        OnPropertyChanged(nameof(MergedPlugins));
        UpdateMergedPlugins();

        _officialIndexMirrorsSubscription = SettingsService.Settings
            .ObservableForProperty(x => x.OfficialIndexMirrors)
            .Subscribe(_ => UpdateOfficialPluginSources());
    }

    /// <summary>
    /// 释放对全局服务和集合的订阅，避免 ViewModel 被静态服务长期保留。页面卸载时调用。
    /// </summary>
    public void Deactivate()
    {
        if (!_isActivated)
        {
            return;
        }

        _isActivated = false;

        _officialIndexMirrorsSubscription?.Dispose();
        _officialIndexMirrorsSubscription = null;

        _mergedPluginsSubscription?.Dispose();
        _mergedPluginsSubscription = null;
        _mergedPluginsFiltered = null!;
        OnPropertyChanged(nameof(MergedPluginsFiltered));

        _mergedPlugins?.Dispose();
        _mergedPlugins = null!;
        OnPropertyChanged(nameof(MergedPlugins));

        OfficialPluginMirrors = null!;
    }

    public void UpdateMergedPlugins()
    {
        if (_mergedPluginsFiltered != null)
            return;

        var pluginFilter = this
            .WhenAnyValue(x => x.PluginFilterText, x => x.PluginCategoryIndex)
            .Select(_ => new Func<KeyValuePair<string, PluginInfo>, bool>(PluginSourceFilter));

        _mergedPluginsSubscription = MergedPlugins.List
            .ToObservableChangeSet()
            .Filter(pluginFilter)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _mergedPluginsFiltered)
            .Subscribe();

        OnPropertyChanged(nameof(MergedPluginsFiltered));
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
