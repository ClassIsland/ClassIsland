using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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

    [ObservableProperty] private SyncDictionaryList<string, string> _officialPluginMirrors = null!;

    public SyncDictionaryList<string, PluginInfo> MergedPlugins { get; }

    private Dictionary<string, int> _randomPluginOrder = new();
    private readonly BehaviorSubject<IComparer<KeyValuePair<string, PluginInfo>>> _sortComparerSubject =
        new(Comparer<KeyValuePair<string, PluginInfo>>.Default);

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

    private void RefreshPluginSortOrder()
    {
        var random = new Random();
        var plugins = MergedPlugins.List.ToList();
        _randomPluginOrder = new(plugins.Count);
        foreach (var (key, _) in plugins)
            _randomPluginOrder[key] = random.Next();
        _sortComparerSubject.OnNext(new PluginInfoComparer(_randomPluginOrder));
    }

    public void UpdateMergedPlugins()
    {
        RefreshPluginSortOrder();

        if (MergedPluginsFiltered != null)
            return;

        var pluginFilter = this
            .WhenAnyValue(x => x.PluginFilterText, x => x.PluginCategoryIndex)
            .Select(_ => new Func<KeyValuePair<string, PluginInfo>, bool>(PluginSourceFilter));

        MergedPlugins.List
            .ToObservableChangeSet()
            .Filter(pluginFilter)
            .Sort(_sortComparerSubject)
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

    private sealed class PluginInfoComparer(Dictionary<string, int> randomOrder)
        : IComparer<KeyValuePair<string, PluginInfo>>
    {
        public int Compare(KeyValuePair<string, PluginInfo> x, KeyValuePair<string, PluginInfo> y)
        {
            var catX = GetCategory(x.Value);
            var catY = GetCategory(y.Value);
            if (catX != catY)
                return catX.CompareTo(catY);
            var randX = randomOrder.GetValueOrDefault(x.Key, 0);
            var randY = randomOrder.GetValueOrDefault(y.Key, 0);
            return randX.CompareTo(randY);
        }

        private static int GetCategory(PluginInfo info)
        {
            var b = info.RestartRequired ? -10 : 0;
            if (info.IsLocal)
            {
                if (info.IsUpdateAvailable)
                    b += info.IsEnabled ? 1 : 2;
                else
                    b += info.IsEnabled ? 3 : 4;
            }
            else
                b += 5;

            return b;
        }
    }
}
