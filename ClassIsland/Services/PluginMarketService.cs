using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
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
using Sentry;

namespace ClassIsland.Services;

public class PluginMarketService(SettingsService settingsService, IPluginService pluginService, ILogger<PluginMarketService> logger) : ObservableRecipient, IPluginMarketService
{
    public static string DefaultPluginIndexKey { get; } = "Default";

    public SettingsService SettingsService { get; } = settingsService;
    public IPluginService PluginService { get; } = pluginService;

    public ObservableDictionary<string, DownloadProgress> DownloadTasks { get; } = new();

    public ObservableDictionary<string, PluginIndex> Indexes { get; } = new();
    public ILogger<PluginMarketService> Logger { get; } = logger;

    public ObservableDictionary<string, string> FallbackMirrors { get; } = new()
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
        var transaction = SentrySdk.StartTransaction("Update Plugin Index", "pluginIndex.update");
        try
        {
            if (SettingsService.Settings.OfficialIndexMirrors.Count <= 0)
            {
                SettingsService.Settings.OfficialIndexMirrors = ConfigureFileHelper.CopyObject(FallbackMirrors);
            }
            var indexes = GetIndexInfos().ToList();
            var i = 0.0;
            var total = Math.Max(1, indexes.Count);
            foreach (var indexInfo in indexes)
            {
                var url = indexInfo.Url.Replace("{time}",
                    ((long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString());
                Logger.LogDebug("正在刷新插件源：{}（{}）", indexInfo.Id, url);
                var archive = Path.GetTempFileName();
                var download = DownloadBuilder.New()
                    .WithUrl(url)
                    .WithFileLocation(archive)
                    .WithConfiguration(new DownloadConfiguration()
                    {
                        Timeout = 10_000
                    })
                    .Build();
                var i1 = i;
                download.DownloadProgressChanged +=
                    (sender, args) =>
                        PluginSourceDownloadProgress = (args.ProgressPercentage / total) + (i1 / total * 100.0);
                download.DownloadFileCompleted += (sender, args) =>
                {
                    if (args.Error != null)
                    {
                        throw new Exception($"无法加载插件源：{args.Error.Message}", args.Error);
                    } 
                    var indexFolderPath = Path.Combine(Services.PluginService.PluginsIndexPath, indexInfo.Id);
                    if (Directory.Exists(indexFolderPath))
                    {
                        Directory.Delete(indexFolderPath, true);
                    }

                    Directory.CreateDirectory(indexFolderPath);
                    ZipFile.ExtractToDirectory(archive, indexFolderPath);
                };
                await download.StartAsync();

                i++;
            }
            LoadPluginSource();
            transaction.Finish(SpanStatus.Ok);
        }
        catch (Exception ex)
        {
            transaction.Finish(ex, SpanStatus.InternalError);
            Logger.LogError(ex, "无法加载插件源。");
            Exception = ex;
        }
        Logger.LogInformation("插件源刷新成功。");
        SettingsService.Settings.LastRefreshPluginSourceTime = DateTime.Now;
        IsLoadingPluginSource = false;
    }

    public IEnumerable<PluginIndexInfo> GetIndexInfos()
    {
        var mirrors = SettingsService.Settings.OfficialIndexMirrors.Count == 0
            ? FallbackMirrors
            : SettingsService.Settings.OfficialIndexMirrors;
        const string repo = "https://get.classisland.tech/d/ClassIsland-Ningbo-S3/classisland/plugin/index.zip?time={time}";
        return SettingsService.Settings.PluginIndexes.Append(new PluginIndexInfo()
        {
            Id = DefaultPluginIndexKey,
            Url = repo,
            SelectedMirror = SettingsService.Settings.OfficialSelectedMirror ?? "github",
            Mirrors = SettingsService.Settings.OfficialIndexMirrors
        });
    }

    public PluginIndexItem? ResolveMarketPlugin(string id)
    {
        return Indexes.Select(i => i.Value.Plugins
            .FirstOrDefault(x => x.Manifest.Id == id))
            .OfType<PluginIndexItem>()
            .FirstOrDefault();
    }

    public async void RequestDownloadPlugin(string id)
    {
        var item = ResolveMarketPlugin(id);
        var transaction = SentrySdk.StartTransaction("Download Plugin", "plugin.download");
        transaction.SetTag("plugin.id", id);

        if (item == null)
        {
            Logger.LogWarning("找不到符合id的插件：{}", id);
            transaction.Finish(SpanStatus.NotFound);
            return;
        }
        transaction.SetTag("plugin", item.Manifest.Name);

        if (DownloadTasks.ContainsKey(id))
        {
            Logger.LogWarning("{}已正在下载。", id);
            transaction.Finish(SpanStatus.AlreadyExists);
            return;
        }

        Logger.LogInformation("开始下载插件：{}", id);
        var spanDownload = transaction.StartChild("download");
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
        transaction.SetTag("url", url);
        if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
        {
            transaction.SetTag("url.host", uri.Host);
        }

        var stopwatch = new Stopwatch();
        download.DownloadFileCompleted += (sender, args) =>
        {
            stopwatch.Stop();
            transaction.SetExtra("download.size", download.TotalFileSize);
            var speed = stopwatch.Elapsed.TotalSeconds == 0
                ? 0.0
                : download.TotalFileSize / stopwatch.Elapsed.TotalSeconds;
            transaction.SetExtra("download.bytesPerSecond", speed);
            if (args.Error != null)
            {
                spanDownload.Finish(args.Error, SpanStatus.InternalError);
                throw new Exception($"无法下载插件 {id}：{args.Error.Message}", args.Error);
            }
            spanDownload.Finish(SpanStatus.Ok);

            var spanValidateChecksum = transaction.StartChild("validate");
            ChecksumHelper.VerifyChecksum(archive, md5);
            spanValidateChecksum.Finish(SpanStatus.Ok);

            var spanMoveToCache = transaction.StartChild("moveToCache");
            File.Move(archive, Path.Combine(Services.PluginService.PluginsPkgRootPath, id + ".cipx"), true);
            spanMoveToCache.Finish(SpanStatus.Ok);
        };
        download.DownloadProgressChanged += (sender, args) =>
        {
            task.Progress = args.ProgressPercentage;
        };
        try
        {
            BindDownloadTasks();
            stopwatch.Start();
            await download.StartAsync(task.CancellationToken);
            item.RestartRequired = true;
            if (MergedPlugins.TryGetValue(id, out var plugin))
            {
                plugin.RestartRequired = true;
            }
            RestartRequested?.Invoke(this, EventArgs.Empty);
            Logger.LogInformation("插件 {} 下载完成。", id);
            transaction.Finish(SpanStatus.Ok);
        }
        catch (Exception e)
        {
            task.Exception = e;
            transaction.GetLastActiveSpan()?.Finish(e, SpanStatus.InternalError);
            transaction.Finish(e, SpanStatus.InternalError);
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
                plugin.DownloadUrl = plugin.DownloadUrl.Replace("{root}", root);
                if (MergedPlugins.ContainsKey(id) && MergedPlugins[id].IsLocal)
                {
                    var pluginLocal = MergedPlugins[id];
                    pluginLocal.IsAvailableOnMarket = true;
                    pluginLocal.DownloadCount = plugin.DownloadCount;
                    pluginLocal.StarsCount = plugin.StarsCount;
                    if (Version.TryParse(pluginLocal.Manifest.Version, out var versionLocal) &&
                        Version.TryParse(plugin.Manifest.Version, out var versionRemote) &&
                        versionRemote > versionLocal)
                    {
                        pluginLocal.IsUpdateAvailable = true;
                    }
                    continue;
                }
                plugin.IsAvailableOnMarket = true;
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