using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Shared;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Downloader;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Sentry;

namespace ClassIsland.Services;

public class PluginMarketService : ObservableRecipient, IPluginMarketService
{
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

    public PluginMarketService(SettingsService settingsService, IPluginService pluginService, ILogger<PluginMarketService> logger, IUriNavigationService uriNavigationService)
    {
        SettingsService = settingsService;
        PluginService = pluginService;
        Logger = logger;
        
        uriNavigationService.HandleAppNavigation("plugin/install", async args =>
        {
            var query = System.Web.HttpUtility.ParseQueryString(args.Uri.Query);
            var pluginId = query["id"];

            if (string.IsNullOrWhiteSpace(pluginId))
            {
                return;
            }

            var pluginPageUri = new Uri($"classisland://app/settings/classisland.plugins?pluginId={pluginId}");

            var confirmBtn = new FluentAvalonia.UI.Controls.TaskDialogButton("确认安装", true)
            {
                IsDefault = true,
                IsEnabled = false,
                IconSource = new ClassIsland.Core.Controls.FluentIconSource("\uE071")
            };
            var viewBtn = new FluentAvalonia.UI.Controls.TaskDialogButton("在商店页查看", false)
            {
                IconSource = new ClassIsland.Core.Controls.FluentIconSource("\uF08B")
            };

            // 构建富内容面板
            var pluginIcon = new AsyncImageLoader.AdvancedImage(new Uri("avares://ClassIsland/"))
            {
                Width = 48,
                Height = 48,
                IsVisible = false,
                Margin = new Avalonia.Thickness(0, 0, 12, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
            };

            var pluginNameText = new Avalonia.Controls.TextBlock()
            {
                Text = pluginId,
                FontSize = 18,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
            };

            var pluginAuthorText = new Avalonia.Controls.TextBlock()
            {
                Text = "",
                FontSize = 13,
                Opacity = 0.7,
                IsVisible = false,
                Margin = new Avalonia.Thickness(0, 2, 0, 0)
            };

            var statusText = new Avalonia.Controls.TextBlock()
            {
                Text = "正在获取插件信息...",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(0, 8, 0, 0)
            };

            var warningText = new Avalonia.Controls.TextBlock()
            {
                Text = "请确认您信任该来源，安装未知插件可能会带来安全风险。",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Opacity = 0.7,
                FontSize = 12,
                IsVisible = false,
                Margin = new Avalonia.Thickness(0, 4, 0, 0)
            };

            var infoPanel = new Avalonia.Controls.StackPanel()
            {
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
            };
            infoPanel.Children.Add(pluginNameText);
            infoPanel.Children.Add(pluginAuthorText);

            var headerPanel = new Avalonia.Controls.DockPanel()
            {
                Margin = new Avalonia.Thickness(0, 0, 0, 0)
            };
            headerPanel.Children.Add(pluginIcon);
            headerPanel.Children.Add(infoPanel);

            var contentPanel = new Avalonia.Controls.StackPanel()
            {
                Width = 380
            };
            contentPanel.Children.Add(headerPanel);
            contentPanel.Children.Add(statusText);
            contentPanel.Children.Add(warningText);

            var dialog = new FluentAvalonia.UI.Controls.TaskDialog()
            {
                Title = "安装插件",
                Header = "确认安装插件？",
                Content = contentPanel,
                XamlRoot = AppBase.Current.GetRootWindow(),
                ShowProgressBar = true,
                Buttons =
                [
                    viewBtn,
                    confirmBtn
                ]
            };

            // 初始加载时显示不确定进度条
            dialog.SetProgressBarState(0, FluentAvalonia.UI.Controls.TaskDialogProgressState.Indeterminate);

            PluginIndexItem? resolvedPluginInfo = null;
            bool isAlreadyInstalled = false;
            bool isDownloading = false;
            bool downloadComplete = false;

            dialog.Opened += async (s, e) =>
            {
                // 尝试置顶弹窗所在窗口
                var rootWindow = AppBase.Current.GetRootWindow();
                rootWindow.Topmost = true;
                rootWindow.Activate();
                rootWindow.Topmost = false;

                var pluginInfo = ResolveMarketPlugin(pluginId);
                if (pluginInfo == null)
                {
                    try
                    {
                        var refreshTask = RefreshPluginSourceAsync();
                        await refreshTask;
                        pluginInfo = ResolveMarketPlugin(pluginId);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "获取插件信息时刷新插件源失败");
                    }
                }

                int retry = 0;
                while (pluginInfo == null && IsLoadingPluginSource && retry < 30)
                {
                    await Task.Delay(500);
                    pluginInfo = ResolveMarketPlugin(pluginId);
                    retry++;
                }

                // 加载完成，隐藏不确定进度条
                dialog.ShowProgressBar = false;

                if (pluginInfo != null)
                {
                    resolvedPluginInfo = pluginInfo;

                    // 显示插件图标
                    if (!string.IsNullOrWhiteSpace(pluginInfo.RealIconPath))
                    {
                        pluginIcon.Source = pluginInfo.RealIconPath;
                        pluginIcon.IsVisible = true;
                    }

                    // 更新插件名称和作者
                    pluginNameText.Text = pluginInfo.Manifest.Name;
                    if (!string.IsNullOrWhiteSpace(pluginInfo.Manifest.Author))
                    {
                        pluginAuthorText.Text = $"作者：{pluginInfo.Manifest.Author}";
                        pluginAuthorText.IsVisible = true;
                    }

                    // 检查插件是否已经安装
                    if (MergedPlugins.TryGetValue(pluginId, out var mergedPlugin) && mergedPlugin.IsLocal)
                    {
                        isAlreadyInstalled = true;
                        dialog.Header = "插件已安装";
                        statusText.Text = $"插件 {pluginInfo.Manifest.Name} 已经安装在本地。";
                        confirmBtn.IsEnabled = false;
                        confirmBtn.Text = "已安装";
                    }
                    else
                    {
                        statusText.Text = $"您正在尝试通过外部链接安装插件 {pluginInfo.Manifest.Name}";
                        warningText.IsVisible = true;
                        confirmBtn.IsEnabled = true;
                    }
                }
                else
                {
                    dialog.Header = "未找到插件";
                    statusText.Text = $"无法在当前配置的插件源中找到插件：\n[{pluginId}]\n\n请检查插件源设置或重试。";
                    confirmBtn.IsEnabled = false;
                    viewBtn.IsEnabled = false;
                }
            };

            dialog.Closing += (s, e) =>
            {
                // 处理"在商店页查看"按钮 - 始终允许导航
                if (Equals(e.Result, false))
                {
                    uriNavigationService.NavigateWrapped(pluginPageUri);
                    return;
                }

                if (Equals(e.Result, true))
                {
                    if (downloadComplete)
                    {
                        // 安装完成，用户点击"重启" => 重启应用
                        AppBase.Current.Restart();
                        return;
                    }

                    if (!isDownloading)
                    {
                        e.Cancel = true;
                        isDownloading = true;

                        confirmBtn.IsEnabled = false;
                        confirmBtn.Text = "正在安装 0%";
                        confirmBtn.IconSource = null;

                        RequestDownloadPlugin(pluginId);

                        _ = Task.Run(async () =>
                        {
                            while (DownloadTasks.ContainsKey(pluginId))
                            {
                                if (DownloadTasks.TryGetValue(pluginId, out var progress))
                                {
                                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        confirmBtn.Text = $"正在安装 {progress.Progress:F0}%";
                                    });
                                }
                                await Task.Delay(200);
                            }

                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                downloadComplete = true;
                                dialog.Header = "插件已安装";
                                statusText.Text = $"插件 {resolvedPluginInfo?.Manifest.Name ?? pluginId} 安装完成，重启应用以加载插件。";
                                warningText.IsVisible = false;
                                // 将确认按钮变更为"重启"并添加重启图标
                                confirmBtn.Text = "重启以应用更新";
                                confirmBtn.IconSource = new ClassIsland.Core.Controls.FluentIconSource("\uE0BD");
                                confirmBtn.IsEnabled = true;
                            });
                        });
                    }
                    else
                    {
                        // 仍在下载中，保持弹窗打开
                        e.Cancel = true;
                    }
                }
            };

            await dialog.ShowAsync();
        });

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
        var ignoreSsl = SettingsService.Settings.IgnoreSslForPluginMirrors;
        var prevCallback = (ignoreSsl ? ServicePointManager.ServerCertificateValidationCallback : null);
        if (ignoreSsl)
        {
            ServicePointManager.ServerCertificateValidationCallback = (_, _, _, _) => true;
        }
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
            var count = MergedPlugins.Count(x => x.Value is { IsUpdateAvailable: true, IsEnabled: true, RestartRequired: false });
            if (count > 0)
            {
                await PlatformServices.DesktopToastService.ShowToastAsync(new DesktopToastContent()
                {
                    Title = "插件更新可用",
                    Body = $"有 {count} 个插件有新版本可用，点击以查看详细信息。",
                    Activated = (_, _) => IAppHost.GetService<IUriNavigationService>().NavigateWrapped(new Uri("classisland://app/settings/classisland.plugins"))
                });
                UpdateAllPlugins();
            }
            transaction.Finish(SpanStatus.Ok);
        }
        catch (Exception ex)
        {
            transaction.Finish(ex, SpanStatus.InternalError);
            Logger.LogError(ex, "无法加载插件源。");
            Exception = ex;
        }
        finally
        {
            if (ignoreSsl)
            {
                ServicePointManager.ServerCertificateValidationCallback = prevCallback;
            }
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

    public void UpdateAllPlugins(bool discardDisabled=false)
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
                    } else if (success > 0 && success < toUpdate.Count)
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
        var ignoreSsl = SettingsService.Settings.IgnoreSslForPluginMirrors;
        var prevCallback = (ignoreSsl ? ServicePointManager.ServerCertificateValidationCallback : null);
        if (ignoreSsl)
        {
            ServicePointManager.ServerCertificateValidationCallback = (_, _, _, _) => true;
        }
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
        finally
        {
            if (ignoreSsl)
            {
                ServicePointManager.ServerCertificateValidationCallback = prevCallback;
            }
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