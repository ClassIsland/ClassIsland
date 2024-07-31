using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Downloader;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class PluginMarketService(SettingsService settingsService, IPluginService pluginService, ILogger<PluginMarketService> logger) : ObservableRecipient, IPluginMarketService
{
    public SettingsService SettingsService { get; } = settingsService;
    public IPluginService PluginService { get; } = pluginService;

    public ObservableDictionary<string, DownloadProgress> DownloadTasks { get; } = new();

    public ObservableDictionary<string, PluginIndex> Indexes { get; } = new();
    public ILogger<PluginMarketService> Logger { get; } = logger;

    private ObservableDictionary<string, PluginInfo> _mergedPlugins = new();
    private bool _isLoadingPluginSource = false;
    private double _pluginSourceDownloadProgress;
    private Exception? _exception;

    public ObservableDictionary<string, PluginInfo> MergedPlugins
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
            LoadPluginSource();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "无法加载插件源：{}", repo);
            Exception = ex;
        }

        IsLoadingPluginSource = false;
    }

    public async void RequestDownloadPlugin(string id)
    {
        PluginIndexItem? item = null;
        foreach (var i in Indexes)
        {
            item = i.Value.Plugins.FirstOrDefault(x => x.Manifest.Id == id);
            if (item != null)
            {
                break;
            }
        }

        if (item == null)
        {
            Logger.LogWarning("找不到符合id的插件：{}", id);
            return;
        }

        if (DownloadTasks.ContainsKey(id))
        {
            Logger.LogWarning("{}已正在下载。", id);
            return;
        }

        Logger.LogInformation("开始下载插件：{}", id);
        // TODO: 使用自定义镜像
        var root = "https://github.moeyy.xyz/https://github.com";
        var url = item.Downloads.First().Value.Replace("{root}", root);
        var md5 = item.DownloadsMd5.First().Value;
        var task = new DownloadProgress()
        {
            IsDownloading = true
        };
        DownloadTasks[id] = task;
        var archive = Path.GetTempFileName() + ".tmp";
        var download = DownloadBuilder.New()
            .WithUrl(url)
            .WithFileLocation(archive)
            .WithConfiguration(new DownloadConfiguration())
            .Build();
        download.DownloadFileCompleted += (sender, args) =>
        {
            ChecksumHelper.VerifyChecksum(archive, md5);
            File.Move(archive, Path.Combine(Services.PluginService.PluginsPkgRootPath, id + ".cipx"), true);
        };
        download.DownloadProgressChanged += (sender, args) =>
        {
            task.Progress = args.ProgressPercentage;
        };
        try
        {
            BindDownloadTasks();
            await download.StartAsync(task.CancellationToken);
            item.RestartRequired = true;
            RestartRequested?.Invoke(this, EventArgs.Empty);
            Logger.LogInformation("插件 {} 下载完成。", id);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "无法从 {} 下载插件 {}", url, id);
        }
        task.IsDownloading = false;
        DownloadTasks.Remove(id);

    }

    public event EventHandler? RestartRequested;

    private void LoadPluginSource()
    {
        MergedPlugins.Clear();
        Indexes.Clear();

        foreach (var pluginLocal in IPluginService.LoadedPlugins)
        {
            var id = pluginLocal.Manifest.Id;
            MergedPlugins[id] = pluginLocal;
        }

        foreach (var i in Directory.EnumerateDirectories(Services.PluginService.PluginsIndexPath))
        {
            var indexPath = Path.Combine(i, "index.json");
            if (!File.Exists(indexPath))
                continue;
            var index = Indexes[i] = ConfigureFileHelper.LoadConfig<PluginIndex>(indexPath);
            foreach (var plugin in index.Plugins)
            {
                var id = plugin.Manifest.Id;
                if (MergedPlugins.ContainsKey(id) && MergedPlugins[id].IsLocal)
                {
                    MergedPlugins[id].IsAvailableOnMarket = true;
                    continue;
                }
                plugin.IsAvailableOnMarket = true;
                MergedPlugins[id] = plugin;
            }
        }

        BindDownloadTasks();


    }

    private void BindDownloadTasks()
    {
        foreach (var i in DownloadTasks)
        {
            var b = MergedPlugins.TryGetValue(i.Key, out var v);
            if (!b || v == null)
                continue;
            v.DownloadProgress = i.Value;
        }
    }
}