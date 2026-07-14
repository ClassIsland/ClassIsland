using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Core.Models.XamlTheme;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Downloader;
using Microsoft.Extensions.Logging;
using Sentry;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ClassIsland.Services;

public class XamlThemeService : ObservableRecipient, IXamlThemeService
{
    private const long MaximumManifestLength = 1024 * 1024;
    private const string InstallStagingPrefix = ".install-";
    private const string InstallBackupPrefix = ".backup-";
    private static readonly FieldInfo? s_stylesAppliedField = typeof(StyledElement).GetField("_stylesApplied", BindingFlags.Instance | BindingFlags.NonPublic);
    
    public ILogger<XamlThemeService> Logger { get; }
    public IPluginMarketService PluginMarketService { get; }
    public SettingsService SettingsService { get; }
    private IComponentsService ComponentsService { get; }
    private Styles RootStyles { get; set; } = [];

    public Window? MainWindow { get; set; }
    
    private Border? ResourceLoaderBorder { get; set; }

    public static readonly string ThemesPath = Path.Combine(CommonDirectories.AppConfigPath, "Themes");
    public static readonly string EnabledThemesPath = Path.Combine(CommonDirectories.AppConfigPath, "EnabledThemes.json");
    public static readonly string ThemesPkgRootPath = Path.Combine(CommonDirectories.AppCacheFolderPath, "ThemePackages");
    private static readonly string ThemesInstallTransactionRootPath = Path.Combine(
        CommonDirectories.AppConfigPath,
        "ThemeInstallTransactions");

    public ObservableCollection<ThemeInfo> Themes { get; } = [];

    public ObservableDictionary<string, ThemeInfo> MergedThemes
    {
        get => _mergedThemes;
        set => SetProperty(ref _mergedThemes, value);
    }

    public ObservableDictionary<string, ThemeIndex> Indexes { get; } = [];

    public ObservableDictionary<string, DownloadProgress> DownloadTasks { get; } = new();

    public ObservableCollection<string> EnabledThemes { get; }

    
    private ObservableDictionary<string, ThemeInfo> _mergedThemes = [];

    public event EventHandler? RestartRequested;

    public double ActualVerticalSafeAreaPx { get; set; } = 0.0;


    public XamlThemeService(ILogger<XamlThemeService> logger, IPluginMarketService pluginMarketService,
        SettingsService settingsService, IComponentsService componentsService)
    {
        Logger = logger;
        PluginMarketService = pluginMarketService;
        SettingsService = settingsService;
        ComponentsService = componentsService;
        EnabledThemes = ConfigureFileHelper.LoadConfig<ObservableCollection<string>>(EnabledThemesPath);
        if (EnabledThemes.Count == 0)
        {
            EnabledThemes.Add("classisland.fluent");
        }
        EnabledThemes.CollectionChanged +=
            (_, _) => ConfigureFileHelper.SaveConfig(EnabledThemesPath, EnabledThemes);
        if (App.ApplicationCommand.Safe)
        {
            return;
        }
        
        ProcessThemeInstall();
        // LoadAllThemes();
        //LoadThemeSource();
    }

    public void LoadAllThemes()
    {
        LoadThemeSource();

        if (App.ApplicationCommand.Safe)
        {
            return;
        }
        ResourceLoaderBorder ??= MainWindow?.FindControl<Border>("ResourceLoaderBorder");
        RootStyles.Clear();
        ResourceLoaderBorder?.Styles.Remove(RootStyles);
        s_stylesAppliedField?.SetValue(ResourceLoaderBorder, false); 
        RootStyles = [];
        ResourceLoaderBorder?.Styles.Add(RootStyles);
        var actualSafeAreaPx = 0.0;
        foreach (var themeInfo in EnabledThemes.Select(x => Themes.FirstOrDefault(y => y.Manifest.Id == x))
                     .OfType<ThemeInfo>())
        {
            try
            {
                if (themeInfo.IsExternal)
                {
                    LoadThemeFromFile(Path.Combine(themeInfo.Path, "Styles.axaml"));
                }
                else
                {
                    LoadThemeFromResource(themeInfo.ThemeUri ?? throw new InvalidOperationException("资源主题必须指定主题 Uri"));
                }
                actualSafeAreaPx = Math.Max(themeInfo.Manifest.VerticalSafeAreaPx, actualSafeAreaPx);
                themeInfo.IsLoaded = true;
            }
            catch (Exception e)
            {
                themeInfo.IsError = true;
                themeInfo.Error = e;
            }
        }

        ActualVerticalSafeAreaPx = actualSafeAreaPx;
    }

    private void LoadThemeFromFile(string themePath)
    {
        Logger.LogInformation("正在从文件加载主题 {}", themePath);
        var uri = new Uri(Path.GetFullPath(themePath));
        if (AvaloniaRuntimeXamlLoader.Load(File.ReadAllText(themePath), Assembly.GetExecutingAssembly(), uri: uri) is
            not Styles styles)
        {
            return;
        }
        RootStyles.Add(styles);
    }
    
    private void LoadThemeFromResource(Uri uri)
    {
        Logger.LogInformation("正在从资源加载主题 {}", uri);
        RootStyles.Add((IStyle)AvaloniaXamlLoader.Load(uri));
    }

    public void LoadThemeSource()
    {
        Logger.LogInformation("正在加载主题源");
        LoadLocalThemes();
        PluginMarketService.LoadPluginSource();
        var merged = new ObservableDictionary<string, ThemeInfo>();
        Indexes.Clear();
        
        foreach (var themeLocal in Themes)
        {
            var id = themeLocal.Manifest.Id;
            merged[id] = themeLocal;
        }


        var indexInfos = PluginMarketService.GetIndexInfos().ToList();
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
            Logger.LogDebug("正在加载主题源：{}", name);
            var indexPath = Path.Combine(indexFolderPath, "themes.json");
            if (!File.Exists(indexPath))
                continue;
            var index = Indexes[name] = ConfigureFileHelper.LoadConfig<ThemeIndex>(indexPath);
            var pluginIndex = PluginMarketService.Indexes.GetValueOrDefault(name) ?? new PluginIndex()
            {
                DownloadMirrors = Services.PluginMarketService.FallbackMirrors
            };
            var mirror = i.SelectedMirror;
            if (!pluginIndex.DownloadMirrors.TryGetValue(mirror, out var root))
            {
                root = pluginIndex.DownloadMirrors.First().Value;
            }
            Logger.LogDebug("主题源 {} 选择的镜像根：{}", name, root);
            foreach (var theme in index.Themes)
            {
                var id = theme.Manifest.Id;
                if (!TryResolveThemePackagePath(id, out _))
                {
                    continue;
                }

                theme.DownloadUrl = theme.DownloadUrl.Replace("{root}", root);
                if (merged.ContainsKey(id) && merged[id].IsLocal)
                {
                    var themeLocal = merged[id];
                    themeLocal.IsAvailableOnMarket = true;
                    themeLocal.DownloadCount = theme.DownloadCount;
                    themeLocal.StarsCount = theme.StarsCount;
                    if (Version.TryParse(themeLocal.Manifest.Version, out var versionLocal) &&
                        Version.TryParse(theme.Manifest.Version, out var versionRemote) &&
                        versionRemote > versionLocal)
                    {
                        themeLocal.IsUpdateAvailable = true;
                    }

                    merged.Remove(id);
                    merged[id] = themeLocal;
                    continue;
                }
                theme.IsAvailableOnMarket = true;
                theme.RealBannerPath = theme.RealBannerPath.Replace("{root}", root);
                merged[id] = theme;
            }
        }

        MergedThemes = merged;
        BindDownloadTasks();
    }

    private void BindDownloadTasks()
    {
        foreach (var i in DownloadTasks)
        {
            var b = MergedThemes.TryGetValue(i.Key, out var v);
            if (!b || v == null)
                continue;
            v.DownloadProgress = i.Value;
        }

        foreach (var theme in MergedThemes)
        {
            if (TryResolveThemePackagePath(theme.Value.Manifest.Id, out var packagePath) &&
                File.Exists(packagePath))
            {
                theme.Value.RestartRequired = true;
            }
        }
    }

    private bool TryResolveThemePackagePath(string id, out string packagePath)
    {
        return TryResolveSafeChildPath(
            ThemesPkgRootPath,
            id,
            ".zip",
            "主题",
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

    private void LoadLocalThemes()
    {
        Themes.Clear();
        foreach (var integratedTheme in IXamlThemeService.IntegratedThemes)
        {
            Themes.Add(integratedTheme);
        }
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        foreach (var i in Directory.GetDirectories(ThemesPath))
        {
            var manifest = new ThemeManifest()
            {
                Name = Path.GetFileName(i),
                Id = Path.GetFileName(i),
            };
            var themeInfo = new ThemeInfo
            {
                Path = Path.GetFullPath(i)
            };
            try
            {
                if (File.Exists(Path.Combine(i, "manifest.yml")))
                {
                    var yaml = File.ReadAllText(Path.Combine(i, "manifest.yml"));
                    manifest = deserializer.Deserialize<ThemeManifest>(yaml);
                }

                themeInfo.Manifest = manifest;
                themeInfo.Path = Path.GetFullPath(i);
                themeInfo.IsLocal = true;
                themeInfo.RealBannerPath = Path.GetFullPath(Path.Combine(themeInfo.Path, themeInfo.Manifest.Banner));
            }
            catch (Exception e)
            {
                themeInfo.IsError = true;
                themeInfo.Error = e;
                Logger.LogError(e, "无法加载主题元数据 {}", i);
            }
            Themes.Add(themeInfo);
        }
    }

    public async void RequestDownloadTheme(string id)
    {
        var item = Indexes.Select(i => i.Value.Themes
                .FirstOrDefault(x => x.Manifest.Id == id))
            .OfType<ThemeIndexItem>()
            .FirstOrDefault();
        var transaction = SentrySdk.StartTransaction("Download Theme", "theme.download");
        transaction.SetTag("theme.id", id);

        if (item == null)
        {
            Logger.LogWarning("找不到符合id的主题：{}", id);
            transaction.Finish(SpanStatus.NotFound);
            return;
        }
        transaction.SetTag("theme", item.Manifest.Name);

        if (DownloadTasks.ContainsKey(id))
        {
            Logger.LogWarning("{}已正在下载。", id);
            transaction.Finish(SpanStatus.AlreadyExists);
            return;
        }

        if (!TryResolveThemePackagePath(id, out var destFileName))
        {
            transaction.Finish(SpanStatus.InternalError);
            return;
        }

        Logger.LogInformation("开始下载主题：{}", id);
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
                throw new Exception($"无法下载主题 {id}：{args.Error.Message}", args.Error);
            }
            spanDownload.Finish(SpanStatus.Ok);

            var spanValidateChecksum = transaction.StartChild("validate");
            ChecksumHelper.VerifyChecksum(archive, md5);
            spanValidateChecksum.Finish(SpanStatus.Ok);

            var spanMoveToCache = transaction.StartChild("moveToCache");
            File.Move(archive, destFileName, true);
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
            if (!Themes.Any(x => x.Manifest.Id == id && EnabledThemes.Contains(id)))
            {
                InstallTheme(destFileName);
                LoadThemeSource();
            }
            else
            {
                if (MergedThemes.TryGetValue(id, out var plugin))
                {
                    plugin.RestartRequired = true;
                }
                item.RestartRequired = true;
                RestartRequested?.Invoke(this, EventArgs.Empty);
            }
            Logger.LogInformation("主题 {} 下载完成。", id);
            transaction.Finish(SpanStatus.Ok);
        }
        catch (Exception e)
        {
            task.Exception = e;
            transaction.GetLastActiveSpan()?.Finish(e, SpanStatus.InternalError);
            transaction.Finish(e, SpanStatus.InternalError);
            Logger.LogError(
                e,
                "无法从主机 {DownloadHost} 下载主题 {ThemeId}",
                downloadHost ?? "未知",
                id);
        }
        task.IsDownloading = false;
        DownloadTasks.Remove(id);
    }

    private static string? GetDownloadHost(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               uri.Scheme is "http" or "https"
            ? uri.IdnHost
            : null;
    }

    private DownloadConfiguration CreateDownloadConfiguration()
    {
        var configuration = new DownloadConfiguration();
        if (SettingsService.Settings.IgnoreSslForPluginMirrors)
        {
            // 兼容选项只能影响当前主题请求，不能修改进程级 TLS 回调。
            configuration.CustomHttpMessageHandlerFactory = static () =>
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
        }

        return configuration;
    }

    private void ProcessThemeInstall()
    {
        if (!Directory.Exists(ThemesPkgRootPath))
        {
            Directory.CreateDirectory(ThemesPkgRootPath);
        }
        if (!Directory.Exists(ThemesPath))
        {
            Directory.CreateDirectory(ThemesPath);
        }
        if (!Directory.Exists(ThemesInstallTransactionRootPath))
        {
            Directory.CreateDirectory(ThemesInstallTransactionRootPath);
        }
        RecoverInterruptedThemeInstalls();

        foreach (var pkgPath in Directory.EnumerateFiles(ThemesPkgRootPath)
                     .Where(x => string.Equals(
                         Path.GetExtension(x),
                         ".zip",
                         StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                InstallTheme(pkgPath);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "无法安装主题 {}", pkgPath);
            }
        }

        foreach (var pkg in Directory.EnumerateDirectories(ThemesPath).Where(x => Path.Exists(Path.Combine(x, ".uninstall"))))
        {
            try
            {
                Directory.Delete(pkg, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "无法卸载主题 {}", pkg);
            }
        }
    }

    private void RecoverInterruptedThemeInstalls()
    {
        foreach (var backupPath in Directory.EnumerateDirectories(ThemesInstallTransactionRootPath)
                     .Where(x => Path.GetFileName(x).StartsWith(
                         InstallBackupPrefix,
                         StringComparison.Ordinal)))
        {
            var id = Path.GetFileName(backupPath)[InstallBackupPrefix.Length..];
            try
            {
                var targetPath = SafeChildDirectoryPath.Resolve(ThemesPath, id);
                if (Directory.Exists(targetPath))
                {
                    TryDeleteDirectory(backupPath);
                }
                else
                {
                    Directory.Move(backupPath, targetPath);
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "无法恢复主题安装事务 {ThemeId}", id);
            }
        }

        foreach (var stagingPath in Directory.EnumerateDirectories(ThemesInstallTransactionRootPath)
                     .Where(x => Path.GetFileName(x).StartsWith(
                         InstallStagingPrefix,
                         StringComparison.Ordinal)))
        {
            TryDeleteDirectory(stagingPath);
        }
    }

    private void InstallTheme(string pkgPath)
    {
        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        string? stagingPath = null;
        try
        {
            using var pkg = ZipFile.OpenRead(pkgPath);
            ZipArchiveSafety.ValidateForExtraction(pkg);
            var mf = pkg.GetEntry("manifest.yml");
            if (mf == null)
            {
                throw new InvalidDataException("主题包缺少 manifest.yml。");
            }
            if (mf.Length > MaximumManifestLength)
            {
                throw new InvalidDataException("主题 manifest.yml 超过大小上限。");
            }

            ThemeManifest manifest;
            using (var manifestReader = new StreamReader(mf.Open()))
            {
                manifest = deserializer.Deserialize<ThemeManifest>(manifestReader.ReadToEnd())
                           ?? throw new InvalidDataException("主题 manifest.yml 内容为空。");
            }

            SafeChildDirectoryPath.ValidateName(manifest.Id);
            var targetPath = SafeChildDirectoryPath.Resolve(ThemesPath, manifest.Id);
            stagingPath = SafeChildDirectoryPath.Resolve(
                ThemesInstallTransactionRootPath,
                $"{InstallStagingPrefix}{manifest.Id}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(stagingPath);
            pkg.ExtractToDirectory(stagingPath);

            var extractedManifestPath = SafeRelativePath.ResolveUnderRoot(
                stagingPath,
                "manifest.yml");
            var extractedStylesPath = SafeRelativePath.ResolveUnderRoot(
                stagingPath,
                "Styles.axaml");
            if (!File.Exists(extractedManifestPath))
            {
                throw new InvalidDataException("主题包解压后缺少 manifest.yml。");
            }
            if (!File.Exists(extractedStylesPath))
            {
                throw new InvalidDataException("主题包解压后缺少 Styles.axaml。");
            }

            ReplaceThemeDirectory(stagingPath, targetPath, manifest.Id);
            stagingPath = null;
        }
        finally
        {
            if (stagingPath != null)
            {
                TryDeleteDirectory(stagingPath);
            }

            try
            {
                File.Delete(pkgPath);
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "无法删除主题安装包 {PackagePath}", pkgPath);
            }
        }
    }

    private void ReplaceThemeDirectory(
        string stagingPath,
        string targetPath,
        string themeId)
    {
        var backupPath = SafeChildDirectoryPath.Resolve(
            ThemesInstallTransactionRootPath,
            $"{InstallBackupPrefix}{themeId}");
        if (Directory.Exists(backupPath))
        {
            if (Directory.Exists(targetPath))
            {
                TryDeleteDirectory(backupPath);
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
                throw new IOException($"主题目标路径不是目录：{targetPath}");
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
            TryDeleteDirectory(backupPath);
        }
    }

    private void TryDeleteDirectory(string path)
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
            Logger.LogWarning(exception, "无法清理临时主题目录 {DirectoryPath}", path);
        }
    }

    public async Task PackageThemeAsync(string id, string outputPath)
    {
        var plugin = Themes.FirstOrDefault(x => x.Manifest.Id == id);
        if (plugin == null)
        {
            throw new ArgumentException($"找不到主题 {id}。", nameof(id));
        }

        await Task.Run(() =>
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            ZipFile.CreateFromDirectory(plugin.Path, outputPath);
        });
    }
}
