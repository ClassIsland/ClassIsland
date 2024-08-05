using System;
using System.Collections.Generic;
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
    public static string DefaultPluginIndexKey { get; } = "Default";

    public SettingsService SettingsService { get; } = settingsService;
    public IPluginService PluginService { get; } = pluginService;

    public ObservableDictionary<string, DownloadProgress> DownloadTasks { get; } = new();

    public ObservableDictionary<string, PluginIndex> Indexes { get; } = new();
    public ILogger<PluginMarketService> Logger { get; } = logger;

    public Dictionary<string, string> FallbackMirrors { get; } = new()
    {
        { "github", "https://github.com" },
        { "ghproxy", "https://mirror.ghproxy.com/https://github.com" },
        { "moeyy", "https://github.moeyy.xyz/https://github.com" }
    };

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
        if (IsLoadingPluginSource)
            return;
        IsLoadingPluginSource = true;
        Exception = null;
        PluginSourceDownloadProgress = 0.0;
        Logger.LogInformation("正在刷新插件源……");
        try
        {
            var indexes = GetIndexInfos().ToList();
            var i = 0.0;
            var total = Math.Max(1, indexes.Count);
            foreach (var indexInfo in indexes)
            {
                Logger.LogDebug("正在刷新插件源：{}（{}）", indexInfo.Id, indexInfo.Url);
                var archive = Path.GetTempFileName() + ".tmp";
                var download = DownloadBuilder.New()
                    .WithUrl(indexInfo.Url)
                    .WithFileLocation(archive)
                    .WithConfiguration(new DownloadConfiguration())
                    .Build();
                var i1 = i;
                download.DownloadProgressChanged +=
                    (sender, args) =>
                        PluginSourceDownloadProgress = args.ProgressPercentage / total + i1 / total * 100.0;
                await download.StartAsync();

                var indexFolderPath = Path.Combine(Services.PluginService.PluginsIndexPath, indexInfo.Id);
                if (Directory.Exists(indexFolderPath))
                {
                    Directory.Delete(indexFolderPath, true);
                }

                Directory.CreateDirectory(indexFolderPath);

                await Task.Run(() => { ZipFile.ExtractToDirectory(archive, indexFolderPath); });
                i++;
            }

            LoadPluginSource();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "无法加载插件源。");
            Exception = ex;
        }
        Logger.LogInformation("插件源刷新成功。");
        SettingsService.Settings.LastRefreshPluginSourceTime = DateTime.Now;
        IsLoadingPluginSource = false;
    }

    private IEnumerable<PluginIndexInfo> GetIndexInfos()
    {
        var mirrors = SettingsService.Settings.OfficialIndexMirrors.Count == 0
            ? FallbackMirrors
            : SettingsService.Settings.OfficialIndexMirrors;
        var repo = "{root}/ClassIsland/PluginIndex/releases/download/latest/index.zip".Replace("{root}", mirrors[SettingsService.Settings.OfficialSelectedMirror]);
        return SettingsService.Settings.PluginIndexes.Append(new PluginIndexInfo()
        {
            Id = DefaultPluginIndexKey,
            Url = repo,
            SelectedMirror = SettingsService.Settings.OfficialSelectedMirror,
            Mirrors = SettingsService.Settings.OfficialIndexMirrors
        });
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
        var url = item.DownloadUrl;
        var md5 = item.DownloadMd5;
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
            task.Exception = e;
            Logger.LogError(e, "无法从 {} 下载插件 {}", url, id);
        }
        task.IsDownloading = false;
        DownloadTasks.Remove(id);

    }

    public event EventHandler? RestartRequested;

    public void LoadPluginSource()
    {
        Logger.LogInformation("正在加载插件源");
        MergedPlugins.Clear();
        Indexes.Clear();

        foreach (var pluginLocal in IPluginService.LoadedPlugins)
        {
            var id = pluginLocal.Manifest.Id;
            MergedPlugins[id] = pluginLocal;
        }

        var indexInfos = GetIndexInfos().ToList();
        foreach (var i in indexInfos)
        {
            var indexFolderPath = Path.Combine(Services.PluginService.PluginsIndexPath, i.Id);
            var name = Path.GetFileName(indexFolderPath);
            Logger.LogDebug("正在加载插件源：{}", name);
            var indexPath = Path.Combine(indexFolderPath, "index.json");
            if (!File.Exists(indexPath))
                continue;
            var index = Indexes[name] = ConfigureFileHelper.LoadConfig<PluginIndex>(indexPath);
            var mirror = i.SelectedMirror;
            i.Mirrors = ConfigureFileHelper.CopyObject(index.DownloadMirrors);
            if (!index.DownloadMirrors.TryGetValue(mirror, out var root))
            {
                mirror = i.SelectedMirror = index.DownloadMirrors.First().Key;
                root = index.DownloadMirrors.First().Value;
            }
            Logger.LogDebug("插件源 {} 选择的镜像根：{}", name, root);
            foreach (var plugin in index.Plugins)
            {
                var id = plugin.Manifest.Id;
                if (MergedPlugins.ContainsKey(id) && MergedPlugins[id].IsLocal)
                {
                    MergedPlugins[id].IsAvailableOnMarket = true;
                    continue;
                }
                plugin.IsAvailableOnMarket = true;
                plugin.DownloadUrl = plugin.DownloadUrl.Replace("{root}", root);
                plugin.RealIconPath = plugin.RealIconPath.Replace("{root}", root);
                plugin.Manifest.Readme = plugin.Manifest.Readme.Replace("{root}", root);
                MergedPlugins[id] = plugin;
            }
        }

        SettingsService.Settings.OfficialSelectedMirror =
            indexInfos.First(x => x.Id == DefaultPluginIndexKey).SelectedMirror;
        var defaultIndex = Indexes.FirstOrDefault(x => x.Key == DefaultPluginIndexKey).Value ?? new PluginIndex();
        SettingsService.Settings.OfficialIndexMirrors = ConfigureFileHelper.CopyObject(
            defaultIndex.DownloadMirrors);
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

        foreach (var plugin in MergedPlugins.Where(plugin => File.Exists(Path.Combine(Services.PluginService.PluginsPkgRootPath, plugin.Value.Manifest.Id + ".cipx"))))
        {
            plugin.Value.RestartRequired = true;
        }
    }
}