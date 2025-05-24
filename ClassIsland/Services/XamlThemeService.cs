using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Core.Models.XamlTheme;
using ClassIsland.Shared;
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
    public ILogger<XamlThemeService> Logger { get; }
    public IPluginMarketService PluginMarketService { get; }
    public SettingsService SettingsService { get; }
    private ResourceDictionary RootResourceDictionary { get; } = new();

    private Window MainWindow { get; } = AppBase.Current.MainWindow!;

    public static readonly string ThemesPath = Path.Combine(App.AppConfigPath, "Themes");

    public ObservableCollection<ThemeInfo> Themes { get; } = [];

    public ObservableDictionary<string, ThemeInfo> MergedThemes
    {
        get => _mergedThemes;
        set => SetProperty(ref _mergedThemes, value);
    }

    public ObservableDictionary<string, ThemeIndex> Indexes { get; } = [];

    public ObservableDictionary<string, DownloadProgress> DownloadTasks { get; } = new();

    public static readonly string ThemesPkgRootPath = Path.Combine(App.AppCacheFolderPath, "ThemePackages");
    private ObservableDictionary<string, ThemeInfo> _mergedThemes = [];

    public event EventHandler? RestartRequested;


    public XamlThemeService(ILogger<XamlThemeService> logger, IPluginMarketService pluginMarketService, SettingsService settingsService)
    {
        Logger = logger;
        PluginMarketService = pluginMarketService;
        SettingsService = settingsService;
        if (App.ApplicationCommand.Safe)
        {
            return;
        }
        var resourceBoarder = VisualTreeUtils.FindChildVisualByName<Border>(MainWindow, "ResourceLoaderBorder");
        resourceBoarder?.Resources.MergedDictionaries.Add(RootResourceDictionary);

        ProcessThemeInstall();
        LoadAllThemes();
        //LoadThemeSource();
    }

    public void LoadAllThemes()
    {
        LoadThemeSource();

        if (App.ApplicationCommand.Safe)
        {
            return;
        }
        RootResourceDictionary.MergedDictionaries.Clear();
        foreach (var themeInfo in Themes)
        {
            if (themeInfo.IsEnabled)
            {
                LoadTheme(Path.Combine(themeInfo.Path, "Theme.xaml"));
                themeInfo.IsLoaded = true;
            }
        }
    }

    public void LoadTheme(string themePath)
    {
        Logger.LogInformation("正在加载主题 {}", themePath);
        var themeResourceDictionary = new ResourceDictionary
        {
            Source = new Uri(Path.GetFullPath(themePath))
        };
        RootResourceDictionary.MergedDictionaries.Add(themeResourceDictionary);
    }

    public void LoadThemeSource()
    {
        Logger.LogInformation("正在加载主题源");
        LoadLocalThemes();
        PluginMarketService.LoadPluginSource();
        var merged = new ObservableDictionary<string, ThemeInfo>();
        Indexes.Clear();

        foreach (var pluginLocal in Themes)
        {
            var id = pluginLocal.Manifest.Id;
            merged[id] = pluginLocal;
        }

        var indexInfos = PluginMarketService.GetIndexInfos().ToList();
        foreach (var i in indexInfos)
        {
            var indexFolderPath = Path.Combine(Services.PluginService.PluginsIndexPath, i.Id);
            var name = Path.GetFileName(indexFolderPath);
            Logger.LogDebug("正在加载主题源：{}", name);
            var indexPath = Path.Combine(indexFolderPath, "themes.json");
            if (!File.Exists(indexPath))
                continue;
            var index = Indexes[name] = ConfigureFileHelper.LoadConfig<ThemeIndex>(indexPath);
            var pluginIndex = PluginMarketService.Indexes[name];
            var mirror = i.SelectedMirror;
            if (!pluginIndex.DownloadMirrors.TryGetValue(mirror, out var root))
            {
                root = pluginIndex.DownloadMirrors.First().Value;
            }
            Logger.LogDebug("主题源 {} 选择的镜像根：{}", name, root);
            foreach (var theme in index.Themes)
            {
                var id = theme.Manifest.Id;
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

        foreach (var theme in MergedThemes.Where(theme => File.Exists(Path.Combine(ThemesPkgRootPath, theme.Value.Manifest.Id + ".zip"))))
        {
            theme.Value.RestartRequired = true;
        }
    }

    private void LoadLocalThemes()
    {
        Themes.Clear();
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
            .WithConfiguration(new DownloadConfiguration())
            .Build();
        transaction.SetTag("url", url);
        if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
        {
            transaction.SetTag("url.host", uri.Host);
        }

        var stopwatch = new Stopwatch();
        var destFileName = Path.Combine(ThemesPkgRootPath, id + ".zip");
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
            if (!Themes.Any(x => x.Manifest.Id == id && x.IsEnabled))
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
            Logger.LogError(e, "无法从 {} 下载主题 {}", url, id);
        }
        task.IsDownloading = false;
        DownloadTasks.Remove(id);
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

        foreach (var pkgPath in Directory.EnumerateFiles(ThemesPkgRootPath).Where(x => Path.GetExtension(x) == ".zip"))
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

    private static void InstallTheme(string pkgPath)
    {
        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        using (var pkg = ZipFile.OpenRead(pkgPath))
        {
            var mf = pkg.GetEntry("manifest.yml");
            if (mf == null)
                return;
            var mfText = new StreamReader(mf.Open()).ReadToEnd();
            var manifest = deserializer.Deserialize<PluginManifest>(mfText);
            var targetPath = Path.Combine(ThemesPath, manifest.Id);
            if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath, true);
            }

            Directory.CreateDirectory(targetPath);
            ZipFile.ExtractToDirectory(pkgPath, targetPath);
        }
        File.Delete(pkgPath);
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