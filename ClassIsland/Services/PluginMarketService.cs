using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using Downloader;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class PluginMarketService(SettingsService settingsService, IPluginService pluginService, ILogger<PluginMarketService> logger) : ObservableRecipient, IPluginMarketService
{
    public SettingsService SettingsService { get; } = settingsService;
    public IPluginService PluginService { get; } = pluginService;
    public ILogger<PluginMarketService> Logger { get; } = logger;

    private ObservableDictionary<string, PluginIndexItem> _mergedPlugins = new();
    private bool _isLoadingPluginSource = false;
    private double _pluginSourceDownloadProgress;
    private Exception? _exception;

    public ObservableDictionary<string, PluginIndexItem> MergedPlugins
    {
        get => _mergedPlugins;
        set
        {
            if (Equals(value, _mergedPlugins)) return;
            _mergedPlugins = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoadingPluginSource
    {
        get => _isLoadingPluginSource;
        set
        {
            if (value == _isLoadingPluginSource) return;
            _isLoadingPluginSource = value;
            OnPropertyChanged();
        }
    }

    public double PluginSourceDownloadProgress
    {
        get => _pluginSourceDownloadProgress;
        set
        {
            if (value.Equals(_pluginSourceDownloadProgress)) return;
            _pluginSourceDownloadProgress = value;
            OnPropertyChanged();
        }
    }

    public Exception? Exception
    {
        get => _exception;
        set
        {
            if (Equals(value, _exception)) return;
            _exception = value;
            OnPropertyChanged();
        }
    }

    public async Task RefreshPluginSourceAsync()
    {
        // TODO: 使用自定义插件源
        var repo = "https://test.market.classisland.tech/ClassIsland/PluginIndex/releases/download/latest/index.zip";
        try
        {

            if (IsLoadingPluginSource)
                return;
            IsLoadingPluginSource = true;
            Exception = null;
            PluginSourceDownloadProgress = 0.0;
            var archive = Path.GetTempFileName() + ".tmp";
            var download = DownloadBuilder.New()
                .WithUrl(repo)
                .WithFileLocation(archive)
                .WithConfiguration(new DownloadConfiguration())
                .Build();
            download.DownloadProgressChanged +=
                (sender, args) =>
                    PluginSourceDownloadProgress = args.ProgressPercentage;
            await download.StartAsync();

            var indexFolderPath = Path.Combine(Services.PluginService.PluginsIndexPath, "Default");
            if (Directory.Exists(indexFolderPath))
            {
                Directory.Delete(indexFolderPath, true);
            }

            Directory.CreateDirectory(indexFolderPath);

            await Task.Run(() => { ZipFile.ExtractToDirectory(archive, indexFolderPath); });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "无法加载插件源：{}", repo);
            Exception = ex;
        }

        IsLoadingPluginSource = false;
    }

    private void LoadPluginSource()
    {
        foreach (var i in Directory.EnumerateDirectories(Services.PluginService.PluginsIndexPath))
        {
            

        }
    }
}