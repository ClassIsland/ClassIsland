using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Helpers;
using ClassIsland.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using Downloader;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Sentry;

namespace ClassIsland.Services;

public class PluginMarketService : ObservableRecipient, IPluginMarketService
{
    private const string IndexStagingPrefix = ".index-staging-";
    private const string IndexBackupPrefix = ".index-backup-";

    public static string DefaultPluginIndexKey { get; } = "Default";

    public SettingsService SettingsService { get; }
    public IPluginService PluginService { get; }

    public ObservableDictionary<string, DownloadProgress> DownloadTasks { get; } = new();

    public ObservableDictionary<string, PluginIndex> Indexes { get; } = new();
    public ILogger<PluginMarketService> Logger { get; }

    public static ObservableDictionary<string, string> FallbackMirrors { get; } = new()
    {
        { "github", "https://github.com" },
        { "ghproxy", "https://mirror.ghproxy.com/https://github.com" },
        { "moeyy", "https://github.moeyy.xyz/https://github.com" }
    };

    private bool _isLoadingPluginSource = false;
    private double _pluginSourceDownloadProgress;
    private Exception? _exception;
    private IDisposable? _pluginsUpdateProgressObserver;
    private readonly OSPlatform _currentOSPlatform =
        OperatingSystem.IsWindows() ? OSPlatform.Windows :
        OperatingSystem.IsLinux() ? OSPlatform.Linux :
        OperatingSystem.IsMacOS() ? OSPlatform.macOS :
        OperatingSystem.IsAndroid() ? OSPlatform.Android :
        OperatingSystem.IsIOS() ? OSPlatform.iOS :
        OSPlatform.Unknown;

    public PluginMarketService(SettingsService settingsService, IPluginService pluginService, ILogger<PluginMarketService> logger)
    {
        SettingsService = settingsService;
        PluginService = pluginService;
        Logger = logger;

        if (DateTime.Now - SettingsService.Settings.LastRefreshPluginSourceTime >= TimeSpan.FromDays(7))
        {
            _ = RefreshPluginSourceAsync();
        }
    }

    public ObservableDictionary<string, PluginInfo> MergedPlugins { get; } = new();

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
            Directory.CreateDirectory(Services.PluginService.PluginsIndexPath);
            RecoverInterruptedPluginIndexInstalls();
            if (SettingsService.Settings.OfficialIndexMirrors.Count <= 0)
            {
                SettingsService.Settings.OfficialIndexMirrors = ConfigureFileHelper.CopyObject(FallbackMirrors);
            }
            var indexes = GetIndexInfos().ToList();
            var i = 0.0;
            var total = Math.Max(1, indexes.Count);
            foreach (var indexInfo in indexes)
            {
                if (!TryResolveSafeChildPath(
                        Services.PluginService.PluginsIndexPath,
                        indexInfo.Id,
                        string.Empty,
                        "插件源",
                        out var indexFolderPath))
                {
                    i++;
                    continue;
                }

                var url = indexInfo.Url.Replace("{time}",
                    ((long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString());
                Logger.LogDebug(
                    "正在刷新插件源：{}（主机：{}）",
                    indexInfo.Id,
                    GetDownloadHost(url) ?? "未知");
                var archive = Path.GetTempFileName();
                var download = DownloadBuilder.New()
                    .WithUrl(url)
                    .WithFileLocation(archive)
                    .WithConfiguration(CreateDownloadConfiguration(
                        blockTimeout: 10_000,
                        httpClientTimeout: 10_000))
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
                    InstallPluginIndexArchive(
                        archive,
                        indexFolderPath,
                        indexInfo.Id);
                };
                try
                {
                    await download.StartAsync();
                }
                finally
                {
                    try
                    {
                        File.Delete(archive);
                    }
                    catch (Exception exception)
                    {
                        Logger.LogWarning(
                            exception,
                            "无法清理插件源临时包");
                    }
                }

                i++;
            }
            LoadPluginSource();
            var count = MergedPlugins.Count(x => x.Value is { IsUpdateAvailable: true, IsEnabled: true, RestartRequired: false });
            if (count > 0)
            {
                if (SettingsService.Settings.IsPluginsUpdateNotificationEnabled)
                {
                    await PlatformServices.DesktopToastService.ShowToastAsync(new DesktopToastContent()
                    {
                        Title = "插件更新可用",
                        Body = $"有 {count} 个插件有新版本可用，点击以查看详细信息。",
                        Activated = (_, _) => IAppHost.GetService<IUriNavigationService>().NavigateWrapped(new Uri("classisland://app/settings/classisland.plugins"))
                    });
                }
                if (SettingsService.Settings.IsPluginsAutoUpdateEnabled)
                {
                    UpdateAllPlugins();
                }
            }
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
        return SettingsService.Settings.PluginIndexes.Where(x => !string.IsNullOrWhiteSpace(x.Url)).Append(new PluginIndexInfo()
        {
            Id = DefaultPluginIndexKey,
            Url = repo,
            SelectedMirror = SettingsService.Settings.OfficialSelectedMirror ?? "github",
            Mirrors = SettingsService.Settings.OfficialIndexMirrors
        });
    }

    public void UpdateAllPlugins(bool discardDisabled = false)
    {
        var toUpdate = MergedPlugins
            .Where(x => x.Value is { IsUpdateAvailable: true, RestartRequired: false }
                        && (x.Value.DownloadProgress == null || x.Value.DownloadProgress.IsDownloading == false)
                        && (discardDisabled || x.Value.IsEnabled))
            .ToImmutableDictionary();
        if (toUpdate.Count <= 0)
        {
            return;
        }
        _pluginsUpdateProgressObserver ??= DownloadTasks.ObservableForProperty(x => x.Count)
            .Subscribe(_ =>
            {
                if (DownloadTasks.Count > 0) return;
                var success = toUpdate.Values.Count(x => x.DownloadProgress?.Exception == null);

                if (SettingsService.Settings.IsPluginsUpdateNotificationEnabled)
                {
                    if (success == toUpdate.Count)
                    {
                        PlatformServices.DesktopToastService.ShowToastAsync(new DesktopToastContent()
                        {
                            Title = "插件更新完成",
                            Body = $"已将 {success} 个插件升级到最新版本，将在下次启动应用时生效。",
                            Buttons =
                            {
                                { "立即重启", () => AppBase.Current.Restart() }
                            }
                        });
                    }
                    else if (success > 0 && success < toUpdate.Count)
                    {
                        PlatformServices.DesktopToastService.ShowToastAsync(new DesktopToastContent()
                        {
                            Title = "插件更新完成",
                            Body = $"已将 {success} 个插件升级到最新版本，{toUpdate.Count - success} 个插件升级失败。将在下次启动应用时生效。",
                            Buttons =
                            {
                                { "立即重启", () => AppBase.Current.Restart() }
                            }
                        });
                    }
                    else
                    {
                        PlatformServices.DesktopToastService.ShowToastAsync(new DesktopToastContent()
                        {
                            Title = "插件更新失败",
                            Body = $"无法更新插件。请检查您的网络设置，或更换插件镜像源，然后再试一遍。"
                        });
                    }

                }

                _pluginsUpdateProgressObserver?.Dispose();
                _pluginsUpdateProgressObserver = null;
            });

        foreach (var (id, _) in toUpdate)
        {
            RequestDownloadPlugin(id);
        }
    }

    public PluginIndexItem? ResolveMarketPlugin(string id)
    {
        return Indexes.Select(i => i.Value.Plugins
            .FirstOrDefault(x => x.Manifest.Id == id))
            .OfType<PluginIndexItem>()
            .FirstOrDefault();
    }

    private bool TryResolvePluginPackagePath(string id, out string packagePath)
    {
        return TryResolveSafeChildPath(
            Services.PluginService.PluginsPkgRootPath,
            id,
            IPluginService.PluginPackageExtension,
            "插件",
            out packagePath);
    }

    private bool TryResolveSafeChildPath(
        string rootPath,
        string id,
        string suffix,
        string itemType,
        out string targetPath)
    {
        try
        {
            SafeChildDirectoryPath.ValidateName(id);
            targetPath = SafeChildDirectoryPath.Resolve(rootPath, $"{id}{suffix}");
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException)
        {
            Logger.LogWarning(
                "拒绝包含不安全 ID 的{ItemType}：{Id}；原因：{Reason}",
                itemType,
                id,
                exception.Message);
            targetPath = string.Empty;
            return false;
        }
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

        if (!TryResolvePluginPackagePath(id, out var destinationPath))
        {
            transaction.Finish(SpanStatus.InternalError);
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
            .WithConfiguration(CreateDownloadConfiguration())
            .Build();
        var downloadHost = GetDownloadHost(url);
        if (downloadHost != null)
        {
            transaction.SetTag("url.host", downloadHost);
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
            File.Move(archive, destinationPath, true);
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
            Logger.LogError(
                e,
                "无法从主机 {DownloadHost} 下载插件 {PluginId}",
                downloadHost ?? "未知",
                id);
        }
        task.IsDownloading = false;
        DownloadTasks.Remove(id);
    }

    private DownloadConfiguration CreateDownloadConfiguration(
        int? blockTimeout = null,
        int? httpClientTimeout = null)
    {
        var configuration = new DownloadConfiguration();
        if (blockTimeout is { } blockTimeoutValue)
        {
            configuration.BlockTimeout = blockTimeoutValue;
        }
        if (httpClientTimeout is { } httpClientTimeoutValue)
        {
            configuration.HttpClientTimeout = httpClientTimeoutValue;
        }
        if (SettingsService.Settings.IgnoreSslForPluginMirrors)
        {
            // 兼容选项只能影响当前插件请求，不能修改进程级 TLS 回调。
            configuration.CustomHttpMessageHandlerFactory = static () =>
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
        }

        return configuration;
    }

    private static string? GetDownloadHost(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               uri.Scheme is "http" or "https"
            ? uri.IdnHost
            : null;
    }

    private void InstallPluginIndexArchive(
        string archivePath,
        string targetPath,
        string indexId)
    {
        string? stagingPath = null;
        try
        {
            using (var archive = ZipFile.OpenRead(archivePath))
            {
                ZipArchiveSafety.ValidateForExtraction(archive);
                stagingPath = SafeChildDirectoryPath.Resolve(
                    Services.PluginService.PluginsIndexPath,
                    $"{IndexStagingPrefix}{indexId}-{Guid.NewGuid():N}");
                Directory.CreateDirectory(stagingPath);
                archive.ExtractToDirectory(stagingPath);
            }

            var indexPath = SafeRelativePath.ResolveUnderRoot(
                stagingPath,
                "index.v2.json");
            if (!File.Exists(indexPath))
            {
                throw new InvalidDataException("插件源压缩包缺少 index.v2.json。");
            }

            ReplacePluginIndexDirectory(
                stagingPath,
                targetPath,
                indexId);
            stagingPath = null;
        }
        finally
        {
            if (stagingPath != null)
            {
                TryDeletePluginIndexDirectory(stagingPath);
            }
        }
    }

    private void ReplacePluginIndexDirectory(
        string stagingPath,
        string targetPath,
        string indexId)
    {
        var backupPath = SafeChildDirectoryPath.Resolve(
            Services.PluginService.PluginsIndexPath,
            $"{IndexBackupPrefix}{indexId}");
        if (Directory.Exists(backupPath))
        {
            if (Directory.Exists(targetPath))
            {
                TryDeletePluginIndexDirectory(backupPath);
            }
            else
            {
                Directory.Move(backupPath, targetPath);
            }
        }

        var targetBackedUp = false;
        try
        {
            if (File.Exists(targetPath))
            {
                throw new IOException($"插件源目标路径不是目录：{targetPath}");
            }
            if (Directory.Exists(targetPath))
            {
                Directory.Move(targetPath, backupPath);
                targetBackedUp = true;
            }

            Directory.Move(stagingPath, targetPath);
        }
        catch
        {
            if (targetBackedUp &&
                !Directory.Exists(targetPath) &&
                Directory.Exists(backupPath))
            {
                Directory.Move(backupPath, targetPath);
            }
            throw;
        }

        if (targetBackedUp)
        {
            TryDeletePluginIndexDirectory(backupPath);
        }
    }

    private void RecoverInterruptedPluginIndexInstalls()
    {
        var rootPath = Services.PluginService.PluginsIndexPath;
        if (!Directory.Exists(rootPath))
        {
            return;
        }

        foreach (var backupPath in Directory.EnumerateDirectories(rootPath)
                     .Where(x => Path.GetFileName(x).StartsWith(
                         IndexBackupPrefix,
                         StringComparison.Ordinal)))
        {
            var indexId = Path.GetFileName(backupPath)[IndexBackupPrefix.Length..];
            try
            {
                var targetPath = SafeChildDirectoryPath.Resolve(rootPath, indexId);
                if (Directory.Exists(targetPath))
                {
                    TryDeletePluginIndexDirectory(backupPath);
                }
                else
                {
                    Directory.Move(backupPath, targetPath);
                }
            }
            catch (Exception exception)
            {
                Logger.LogWarning(
                    exception,
                    "无法恢复插件源安装事务 {PluginIndexId}",
                    indexId);
            }
        }

        foreach (var stagingPath in Directory.EnumerateDirectories(rootPath)
                     .Where(x => Path.GetFileName(x).StartsWith(
                         IndexStagingPrefix,
                         StringComparison.Ordinal)))
        {
            TryDeletePluginIndexDirectory(stagingPath);
        }
    }

    private void TryDeletePluginIndexDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch (Exception exception)
        {
            Logger.LogWarning(
                exception,
                "无法清理插件源事务目录 {DirectoryPath}",
                path);
        }
    }

    public event EventHandler? RestartRequested;

    public void LoadPluginSource()
    {
        Logger.LogInformation("正在加载插件源");
        Directory.CreateDirectory(Services.PluginService.PluginsIndexPath);
        RecoverInterruptedPluginIndexInstalls();
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
            if (!TryResolveSafeChildPath(
                    Services.PluginService.PluginsIndexPath,
                    i.Id,
                    string.Empty,
                    "插件源",
                    out var indexFolderPath))
            {
                continue;
            }

            var name = Path.GetFileName(indexFolderPath);
            Logger.LogDebug("正在加载插件源：{}", name);
            var indexPath = Path.Combine(indexFolderPath, "index.v2.json");
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
            foreach (var plugin in index.Plugins.Where(x =>
                         Version.TryParse(x.Manifest.ApiVersion, out var version) &&
                         version >= Version.Parse("2.0.0.0")))
            {
                var id = plugin.Manifest.Id;
                if (!TryResolvePluginPackagePath(id, out _))
                {
                    continue;
                }

                plugin.DownloadUrl = plugin.DownloadUrl.Replace("{root}", root);
                if (MergedPlugins.ContainsKey(id) && MergedPlugins[id].IsLocal)
                {
                    var pluginLocal = MergedPlugins[id];
                    pluginLocal.IsAvailableOnMarket = true;
                    pluginLocal.DownloadCount = plugin.DownloadCount;
                    pluginLocal.StarsCount = plugin.StarsCount;
                    if (Version.TryParse(pluginLocal.Manifest.Version, out var versionLocal) &&
                        Version.TryParse(plugin.Manifest.Version, out var versionRemote) &&
                        Version.TryParse(plugin.Manifest.ApiVersion, out var apiVersion) &&
                        Version.TryParse(AppBase.AppVersion, out var appVersion) &&
                        versionRemote > versionLocal)  // TODO: 在 2.0 发布后，添加 api 版本校验！
                    {
                        pluginLocal.IsUpdateAvailable = true;
                    }
                    continue;
                }
                plugin.IsAvailableOnMarket = true;
                plugin.RealIconPath = plugin.RealIconPath.Replace("{root}", root);
                plugin.Manifest.Readme = plugin.Manifest.Readme.Replace("{root}", root);
                plugin.IsNotSupportCurrentOS = !plugin.Manifest.SupportedOSPlatforms.Contains(_currentOSPlatform);
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

        foreach (var plugin in MergedPlugins)
        {
            if (TryResolvePluginPackagePath(plugin.Value.Manifest.Id, out var packagePath) &&
                File.Exists(packagePath))
            {
                plugin.Value.RestartRequired = true;
            }
        }
    }
}
