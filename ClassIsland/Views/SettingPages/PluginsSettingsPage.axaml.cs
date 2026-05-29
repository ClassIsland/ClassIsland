using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.ViewModels.SettingsPages;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using ReactiveUI;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using Path = System.IO.Path;
using System.Runtime.InteropServices;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// PluginsSettingsPage.xaml 的交互逻辑
/// </summary>
///
[FullWidthPage]
[SettingsPageInfo("classisland.plugins", "插件", "\ue071", "\ue071", SettingsPageCategory.External)]
public partial class PluginsSettingsPage : SettingsPageBase
{
    public PluginsSettingsPageViewModel ViewModel { get; } = IAppHost.GetService<PluginsSettingsPageViewModel>();

    public ILogger<PluginsSettingsPage> Logger => ViewModel.Logger;

    private IStorageProvider? StorageProvider => TopLevel.GetTopLevel(this)?.StorageProvider;

    private CancellationTokenSource DocumentLoadingCancellationTokenSource { get; set; } = new();

    public PluginsSettingsPage()
    {
        InitializeComponent();
        DataContext = this;

        ViewModel.PluginMarketService.ObservableForProperty(x => x.Exception)
            .Subscribe(_ =>
            {
                if (ViewModel.PluginMarketService.Exception != null)
                {
                    this.ShowErrorToast("无法加载插件源", ViewModel.PluginMarketService.Exception);
                }
            });
        if (DateTime.Now - ViewModel.SettingsService.Settings.LastRefreshPluginSourceTime >= TimeSpan.FromDays(7))
        {
            _ = ViewModel.PluginMarketService.RefreshPluginSourceAsync();
        }
    }

    private async Task UpdateReadmeDocument()
    {
        if (ViewModel.SelectedPluginInfo == null)
        {
            ViewModel.ReadmeDocument = "";
            return;
        }
        var path = System.IO.Path.Combine(ViewModel.SelectedPluginInfo.PluginFolderPath,
            ViewModel.SelectedPluginInfo.Manifest.Readme);
        var uri = new Uri(path, UriKind.RelativeOrAbsolute);

        string document;
        try
        {
            ViewModel.ReadmeDocument = "> Loading...";
            await DocumentLoadingCancellationTokenSource.CancelAsync();
            DocumentLoadingCancellationTokenSource = new();
            ViewModel.IsLoadingDocument = true;
            document = uri.Scheme switch
            {
                "https" or "http" => await new HttpClient().GetStringAsync(uri,
                    DocumentLoadingCancellationTokenSource.Token),
                "file" => await File.ReadAllTextAsync(path, DocumentLoadingCancellationTokenSource.Token),
                _ => ""
            };
        }
        catch (TaskCanceledException)
        {
            return;
        }
        catch (Exception e)
        {
            document = $"> 无法加载文档：{e.Message}";
        }
        finally
        {
            ViewModel.IsLoadingDocument = false;
        }

        ViewModel.ReadmeDocument = document;
    }

    private async void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ViewModel.SelectedPluginInfo):
                await UpdateReadmeDocument();
                break;
        }
    }
    

    private void ButtonUninstall_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPluginInfo == null)
            return;
        ViewModel.SelectedPluginInfo.IsUninstalling = true;
        RequestRestart();
    }

    private void ButtonUndoUninstall_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPluginInfo == null)
            return;
        ViewModel.SelectedPluginInfo.IsUninstalling = false;
    }

    private async void MenuItemPackPlugin_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPluginInfo == null || StorageProvider == null)
            return;
        PopupHelper.DisableAllPopups();
        var file = await PlatformServices.FilePickerService.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = "打包插件",
            FileTypeChoices = [
                IPluginService.PluginPackageFileType
            ],
            SuggestedFileName = ViewModel.SelectedPluginInfo.Manifest.Id + IPluginService.PluginPackageExtension
        }, TopLevel.GetTopLevel(this) ?? AppBase.Current.GetRootWindow());
        PopupHelper.RestoreAllPopups();
        
        if (file == null)
            return;
        try
        {
            await Services.PluginService.PackagePluginAsync(ViewModel.SelectedPluginInfo.Manifest.Id, file);
            Process.Start(new ProcessStartInfo()
            {
                FileName = Path.GetDirectoryName(file) ?? "",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            this.ShowErrorToast($"无法打包插件 {ViewModel.SelectedPluginInfo.Manifest.Id}", ex);
        }
    }

    private void MenuItemOpenPluginFolder_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPluginInfo == null)
            return;
        Process.Start(new ProcessStartInfo()
        {
            FileName = ViewModel.SelectedPluginInfo.PluginFolderPath,
            UseShellExecute = true
        });
    }

    public class PluginInstallPreviewItem : PluginManifest
    {
        public Avalonia.Media.Imaging.Bitmap? IconBitmap { get; set; }
        public string? SourcePath { get; set; }
    }

    private sealed record PluginInstallPreviewRaw(PluginManifest Manifest, string SourcePath, byte[]? IconBytes);

    private async Task<List<PluginInstallPreviewItem>> GetPluginManifestsAsync(IEnumerable<string> fileNames)
    {
        var raws = await Task.Run(() =>
        {
            var result = new List<PluginInstallPreviewRaw>();

            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithTypeConverter(new OSPlatformTypeConverter())
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            foreach (var fileName in fileNames)
            {
                try
                {
                    using var pkg = ZipFile.OpenRead(fileName);
                    var mf = pkg.GetEntry(Services.PluginService.PluginManifestFileName);
                    if (mf == null)
                        continue;

                    using var reader = new StreamReader(mf.Open());
                    var mfText = reader.ReadToEnd();
                    var manifest = deserializer.Deserialize<PluginManifest>(mfText);
                    if (manifest == null)
                        continue;

                    byte[]? iconBytes = null;

                    var iconPath = manifest.Icon?.Replace('\\', '/').TrimStart('/');
                    if (!string.IsNullOrWhiteSpace(iconPath))
                    {
                        var iconEntry = pkg.GetEntry(iconPath);
                        if (iconEntry != null)
                        {
                            try
                            {
                                using var iconStream = iconEntry.Open();
                                using var ms = new MemoryStream();
                                iconStream.CopyTo(ms);
                                iconBytes = ms.ToArray();
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning(ex, "无法加载插件图标：{Icon}", manifest.Icon);
                            }
                        }
                    }
                    result.Add(new PluginInstallPreviewRaw(manifest, fileName, iconBytes));
                }
                catch (InvalidDataException)
                {
                Logger.LogWarning("不是有效插件包：{File}", fileName);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "无法读取插件清单：{File}", fileName);
                }
            }

            return result;
        });

        var manifests = new List<PluginInstallPreviewItem>();
        foreach (var raw in raws)
        {
            var m = raw.Manifest;
            var item = new PluginInstallPreviewItem()
            {
                EntranceAssembly = m.EntranceAssembly,
                Name = m.Name,
                Id = m.Id,
                Description = m.Description,
                Icon = m.Icon,
                Readme = m.Readme,
                Url = m.Url,
                Version = m.Version,
                ApiVersion = m.ApiVersion,
                Author = m.Author,
                Dependencies = m.Dependencies?.ToList() ?? [],
                SupportedOSPlatforms = m.SupportedOSPlatforms?.ToList() ?? [],
                SourcePath = raw.SourcePath
            };

            if (raw.IconBytes is { Length: > 0 })
            {
                try
                {
                    using var ms = new MemoryStream(raw.IconBytes);
                    item.IconBitmap = new Avalonia.Media.Imaging.Bitmap(ms);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Bitmap 创建失败：{Name}", item.Name);
                }
            }

            manifests.Add(item);
        }
        return manifests;
    }

    private async Task ProcessInstallFiles(IEnumerable<string> filePaths)
    {
        if (ViewModel.SettingsService.Settings.IsPluginMarketWarningVisible)
            return;

        var paths = filePaths
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(File.Exists)
            .Where(x => Path.GetExtension(x).Equals(IPluginService.PluginPackageExtension, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!paths.Any())
        {
            // this.ShowWarningToast($"请选择有效的 {IPluginService.PluginPackageExtension} 插件包文件。");
            return;
        }

        ViewModel.IsInstallingLocalPlugin = true;
        List<PluginInstallPreviewItem> manifests;
        manifests = await GetPluginManifestsAsync(paths);
        if (manifests.Count == 0)
        {
            this.ShowWarningToast("未能从选择的文件中解析出任何可安装的插件包。");
            return;
        }

        var contentTemplate = this.TryFindResource("InstallPluginConfirmTemplate", out var templateObj)
            ? templateObj as IDataTemplate
            : null;

        var dialog = new ContentDialog()
        {
            Title = "确认安装?",
            Content = manifests,
            ContentTemplate = contentTemplate,
            PrimaryButtonText = "安装",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        };

        var topLevel = TopLevel.GetTopLevel(this) ?? AppBase.Current.GetRootWindow();
        ViewModel.IsInstallingLocalPlugin = false;
        if (topLevel == null)
        {
            this.ShowErrorToast("找不到父窗口根节点");
            return;
        }

        var result = await dialog.ShowAsync(topLevel);
        if (result != ContentDialogResult.Primary)
            return;

        int success = 0;
        int failed = 0;
        foreach (var m in manifests)
        {
            var path = m.SourcePath;
            if (string.IsNullOrWhiteSpace(path))
            {
                failed++;
                continue;
            }
            path = path.Trim();
            if (!File.Exists(path))
            {
                this.ShowWarningToast($"文件不存在：{path}");
                failed++;
                continue;
            }
            try
            {
                var dest = Path.Combine(
                    Services.PluginService.PluginsPkgRootPath,
                    Path.GetFileName(path));
                File.Copy(path, dest, true);
                success++;
            }
            catch (Exception ex)
            {
                failed++;
                this.ShowErrorToast($"无法安装插件 {path}", ex);
            }
        }
        if (success > 0)
        {
            this.ShowSuccessToast($"成功安装了 {success} 个插件。");
            RequestRestart();
        }
        else if (failed > 0)
        {
        this.ShowWarningToast($"安装失败：{failed} 个插件。");
        }
    }

    private async void ButtonInstallFromLocal_OnClick(object sender, RoutedEventArgs e)
    {
        if (StorageProvider == null)
        {
            return;
        }
        ViewModel.IsPluginMarketOperationsPopupOpened = false;
        PopupHelper.DisableAllPopups();
        var file = await PlatformServices.FilePickerService.OpenFilesPickerAsync(new FilePickerOpenOptions()
        {
            Title = "从本地安装插件",
            FileTypeFilter = [IPluginService.PluginPackageFileType],
            AllowMultiple = true
        }, TopLevel.GetTopLevel(this) ?? AppBase.Current.GetRootWindow());
        PopupHelper.RestoreAllPopups();
        if (file == null || file.Count == 0)
            return;

        await ProcessInstallFiles(file);
    }

    private void MenuItemOpenPluginConfigFolder_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPluginInfo == null)
            return;
        Process.Start(new ProcessStartInfo()
        {
            FileName = Path.Combine(Services.PluginService.PluginConfigsFolderPath, ViewModel.SelectedPluginInfo.Manifest.Id),
            UseShellExecute = true
        });
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsPluginOperationsPopupOpened = false;
    }

    private async void ButtonBaseRefreshPlugins_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.PluginMarketService.RefreshPluginSourceAsync();
    }

    private async void ButtonInstallPlugin_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPluginInfo == null)
            return;
        await InstallPlugin(ViewModel.SelectedPluginInfo.Manifest.Id);
    }

    [RelayCommand]
    private async Task InstallPlugin(string id)
    {
        List<PluginInfo> resolvedPlugins = [];
        List<string> missingPlugins = [];
        var plugin = ViewModel.PluginMarketService.ResolveMarketPlugin(id);
        if (plugin == null)
        {
            Logger.LogWarning("未找到插件：{}", id);
            return;
        }

        resolvedPlugins.Add(plugin);
        if (plugin.IsNotSupportCurrentOS)
        {
            var result = await new ContentDialog()
            {
                Title = "操作系统不受该插件支持",
                Content = "此插件所声明支持的操作系统并未包括当前所运行的操作系统。" + Environment.NewLine + "如果继续安装此插件，此插件将可能无法正常工作。您要继续安装此插件吗？",
                SecondaryButtonText = "取消",
                PrimaryButtonText = "继续",
                DefaultButton = ContentDialogButton.Secondary
            }.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }
        }
        ResolveDependencies(plugin, resolvedPlugins, missingPlugins);
        if (missingPlugins.Count > 0)
        {
            var result = await new ContentDialog()
            {
                Title = "缺少依赖项",
                Content = "此插件的部分必选依赖项未安装且无法从市场获取。如果继续安装此插件，此插件将可能无法工作。您要继续安装此插件吗？" +Environment.NewLine +Environment.NewLine +
                          "未找到的必选依赖项："+Environment.NewLine + string.Join(Environment.NewLine, missingPlugins),
                SecondaryButtonText = "取消",
                PrimaryButtonText = "继续",
                DefaultButton = ContentDialogButton.Secondary
            }.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }
        }
        foreach (var i in resolvedPlugins)
        {
            ViewModel.PluginMarketService.RequestDownloadPlugin(i.Manifest.Id);
        }
    }

    private void ResolveDependencies(PluginInfo plugin, List<PluginInfo> resolvedPlugins, List<string> missingPlugins)
    {
        if (IPluginService.LoadedPluginsIds.Contains(plugin.Manifest.Id) || resolvedPlugins.Contains(plugin))
        {
            return;
        }
        resolvedPlugins.Add(plugin);
        foreach (var i in plugin.Manifest.Dependencies)
        {
            var dep = ViewModel.PluginMarketService.ResolveMarketPlugin(i.Id);
            if (dep == null)
            {
                if (i.IsRequired && !IPluginService.LoadedPluginsIds.Contains(i.Id))
                {
                    missingPlugins.Add(i.Id);
                }
                continue;
            }
            ResolveDependencies(dep, resolvedPlugins, missingPlugins);
        }
    }

    private void MenuItemReloadFromCache_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsPluginMarketOperationsPopupOpened = false;
        ViewModel.PluginMarketService.LoadPluginSource();
    }

    [RelayCommand]
    private void OpenPluginSourceManager()
    {
        ViewModel.IsPluginMarketOperationsPopupOpened = false;
        OpenDrawer("PluginSourceManageDrawer");
    }

    private void MenuItemOpenPluginsFolder_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsPluginMarketOperationsPopupOpened = false;
        Process.Start(new ProcessStartInfo()
        {
            FileName = Path.GetFullPath(Services.PluginService.PluginsRootPath),
            UseShellExecute = true
        });
    }

    private void ButtonBase2_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsPluginMarketOperationsPopupOpened = false;
    }

    private void ButtonAddPluginSource_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.PluginIndexes.Add(new PluginIndexInfo());
    }

    private void ButtonRemovePluginSource_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPluginIndexInfo == null)
            return;
        ViewModel.SettingsService.Settings.PluginIndexes.Remove(ViewModel.SelectedPluginIndexInfo);
    }

    private void ListBoxCategory_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.UpdateMergedPlugins();
    }
    

    private void TextBoxFilter_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        Focus();
        ViewModel.UpdateMergedPlugins();
    }

    private void ButtonRestart_OnClick(object sender, RoutedEventArgs e)
    {
        RequestRestart();
    }

    private void ButtonAgreePluginNotice_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.IsPluginMarketWarningVisible = false;
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.SelectedPluginInfo == null)
        {
            ViewModel.IsDetailsShown = false;
            return;
        }

        ViewModel.IsDetailsShown = true;
    }
    private void Grid_DragEnter(object sender, DragEventArgs e)
    {
        var files = e.Data.GetFiles()?.Select(x => x.Path.LocalPath).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        if (files == null || files.Count == 0 || ViewModel.SettingsService.Settings.IsPluginMarketWarningVisible)
        {
            ViewModel.IsDragEntering = false;
            e.DragEffects = DragDropEffects.None;
            return;
        }

        var supported = files.Count(x => Path.GetExtension(x).Equals(IPluginService.PluginPackageExtension, StringComparison.OrdinalIgnoreCase));
        ViewModel.IsDragEntering = true;
        ViewModel.DragInstallTotalCount = files.Count;
        ViewModel.DragInstallSupportedCount = supported;
        ViewModel.IsDragInstallValid = supported > 0;

        if (supported <= 0)
        {
            ViewModel.DragInstallHintText = $"仅支持 {IPluginService.PluginPackageExtension} 插件包";
            ViewModel.DragInstallSubHintText = "请拖入插件包文件";
            e.DragEffects = DragDropEffects.None;
            return;
        }

        ViewModel.DragInstallHintText = supported == 1 ? "松开以安装 1 个插件" : $"松开以安装 {supported} 个插件";
        var ignored = files.Count - supported;
        ViewModel.DragInstallSubHintText = ignored > 0 ? $"将忽略 {ignored} 个不受支持的文件" : $"支持多选（{IPluginService.PluginPackageExtension}）";
        e.DragEffects = DragDropEffects.Copy;
    }
    private async void Grid_Drop(object sender, DragEventArgs e)
    {
        ViewModel.IsDragEntering = false;
        if (ViewModel.SettingsService.Settings.IsPluginMarketWarningVisible)
            return;

        var files = e.Data.GetFiles()?.Select(x => x.Path.LocalPath).ToList();
        if (files == null || files.Count == 0)
            return;

        await ProcessInstallFiles(files.Where(x => Path.GetExtension(x).Equals(IPluginService.PluginPackageExtension, StringComparison.OrdinalIgnoreCase)));
    }

    private void Grid_DragLeave(object sender, DragEventArgs e)
    {
        ViewModel.IsDragEntering = false;
        ViewModel.IsDragInstallValid = false;
    }

    private void PluginsSettingsPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        ViewModel.PluginMarketService.RestartRequested += OnPluginMarketServiceOnRestartRequested;

        // 处理 pluginId 查询参数以选中指定插件
        if (NavigationUri != null)
        {
            var queryParams = System.Web.HttpUtility.ParseQueryString(NavigationUri.Query);
            var targetPluginId = queryParams["pluginId"];
            if (!string.IsNullOrWhiteSpace(targetPluginId))
            {
                // 切换到商店页以显示插件
                ViewModel.PluginCategoryIndex = 0;
                ViewModel.UpdateMergedPlugins();

                if (ViewModel.PluginMarketService.MergedPlugins.TryGetValue(targetPluginId, out var targetPlugin))
                {
                    ViewModel.SelectedPluginInfo = targetPlugin;
                }
            }
        }
    }

    private void OnPluginMarketServiceOnRestartRequested(object? sender, EventArgs args)
    {
        if (ViewModel.PluginMarketService.MergedPlugins.Any(x => x.Value.DownloadProgress?.IsDownloading == true))
        {
            return;
        }
        RequestRestart();
    }

    private void PluginsSettingsPage_OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        ViewModel.PluginMarketService.RestartRequested -= OnPluginMarketServiceOnRestartRequested;
    }

    private void ButtonOpenMarket_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.PluginCategoryIndex = 0;
    }

    private void MenuItemManagePluginSources_OnClick(object? sender, RoutedEventArgs e)
    {
        // 这里清除掉来自 PopupBase 的调用堆栈，防止出现打开抽屉命令执行事件传播错误的问题。
        Dispatcher.UIThread.InvokeAsync(OpenPluginSourceManager);
    }

    private void MenuItemPluginUpdateSettings_OnClick(object? sender, RoutedEventArgs e)
    {
        // 这里清除掉来自 PopupBase 的调用堆栈，防止出现打开抽屉命令执行事件传播错误的问题。
        Dispatcher.UIThread.InvokeAsync(() => OpenDrawer("PluginUpdateSettingsDrawer"));
    }
}

