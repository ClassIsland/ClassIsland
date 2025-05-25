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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Services;
using ClassIsland.ViewModels.SettingsPages;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using CommonDialog = ClassIsland.Core.Controls.CommonDialog.CommonDialog;
using Path = System.IO.Path;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// PluginsSettingsPage.xaml 的交互逻辑
/// </summary>
///
[SettingsPageInfo("classisland.plugins", "插件", PackIconKind.ToyBrickOutline, PackIconKind.ToyBrick, SettingsPageCategory.External)]
public partial class PluginsSettingsPage : SettingsPageBase
{
    public PluginsSettingsPageViewModel ViewModel { get; } = new();

    public IPluginService PluginService { get; }
    public IPluginMarketService PluginMarketService { get; }
    public SettingsService SettingsService { get; }
    public ILogger<PluginsSettingsPage> Logger { get; }

    private CancellationTokenSource DocumentLoadingCancellationTokenSource { get; set; } = new();

    public PluginsSettingsPage(IPluginService pluginService, IPluginMarketService pluginMarketService, SettingsService settingsService, ILogger<PluginsSettingsPage> logger)
    {
        InitializeComponent();
        DataContext = this;
        PluginService = pluginService;
        PluginMarketService = pluginMarketService;
        SettingsService = settingsService;
        Logger = logger;

        if (DateTime.Now - SettingsService.Settings.LastRefreshPluginSourceTime >= TimeSpan.FromDays(7))
        {
            _ = PluginMarketService.RefreshPluginSourceAsync();
        }
    }

    private async Task UpdateReadmeDocument()
    {
        if (ViewModel.SelectedPluginInfo == null)
        {
            ViewModel.ReadmeDocument = new FlowDocument();
            return;
        }
        var path = System.IO.Path.Combine(ViewModel.SelectedPluginInfo.PluginFolderPath,
            ViewModel.SelectedPluginInfo.Manifest.Readme);
        var uri = new Uri(path, UriKind.RelativeOrAbsolute);

        string document;
        try
        {
            ViewModel.ReadmeDocument = MarkdownConvertHelper.ConvertMarkdown("> Loading...");
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

        ViewModel.ReadmeDocument = MarkdownConvertHelper.ConvertMarkdown(document);
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

    private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!e.Handled)
        {
            // ListView拦截鼠标滚轮事件
            e.Handled = true;

            // 激发一个鼠标滚轮事件，冒泡给外层ListView接收到
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            eventArg.Source = sender;
            var parent = ((System.Windows.Controls.Control)sender).Parent as UIElement;
            if (parent != null)
            {
                parent.RaiseEvent(eventArg);
            }
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
        if (ViewModel.SelectedPluginInfo == null)
            return;
        var dialog = new SaveFileDialog()
        {
            Title = "打包插件",
            FileName = ViewModel.SelectedPluginInfo.Manifest.Id + IPluginService.PluginPackageExtension,
            Filter = $"ClassIsland 插件包(*{IPluginService.PluginPackageExtension})|*{IPluginService.PluginPackageExtension}"
        };
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;
        try
        {
            await Services.PluginService.PackagePluginAsync(ViewModel.SelectedPluginInfo.Manifest.Id, dialog.FileName);
            Process.Start(new ProcessStartInfo()
            {
                FileName = Path.GetDirectoryName(dialog.FileName) ?? "",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            CommonDialog.ShowError($"无法打包插件 {ViewModel.SelectedPluginInfo.Manifest.Id}：{ex.Message}");
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

    private void ButtonInstallFromLocal_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsPluginMarketOperationsPopupOpened = false;
        var dialog = new OpenFileDialog()
        {
            Title = "从本地安装插件",
            Filter = $"ClassIsland 插件包(*{IPluginService.PluginPackageExtension})|*{IPluginService.PluginPackageExtension}"
        };
        if (dialog.ShowDialog() != true)
            return;
        try
        {
            File.Copy(dialog.FileName, Path.Combine(Services.PluginService.PluginsPkgRootPath, Path.GetFileName(dialog.FileName)), true);
            RequestRestart();
        }
        catch (Exception exception)
        {
            CommonDialog.ShowError($"无法安装插件：{exception.Message}");
        }
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
        await PluginMarketService.RefreshPluginSourceAsync();
        if (FindResource("PluginSource") is CollectionViewSource source)
        {
            source.View.Refresh();
        }
    }

    private void ButtonInstallPlugin_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPluginInfo == null)
            return;
        InstallPlugin(ViewModel.SelectedPluginInfo.Manifest.Id);
    }

    [RelayCommand]
    private void InstallPlugin(string id)
    {
        List<PluginInfo> resolvedPlugins = [];
        List<string> missingPlugins = [];
        var plugin = PluginMarketService.ResolveMarketPlugin(id);
        if (plugin == null)
        {
            Logger.LogWarning("未找到插件：{}", id);
            return;
        }

        resolvedPlugins.Add(plugin);
        ResolveDependencies(plugin, resolvedPlugins, missingPlugins);
        if (missingPlugins.Count > 0)
        {
            var result = new CommonDialogBuilder()
                .SetIconKind(CommonDialogIconKind.Hint)
                .SetContent("此插件的部分必选依赖项未安装且无法从市场获取。如果继续安装此插件，此插件将可能无法工作。您要继续安装此插件吗？\n\n" +
                            "未找到的必选依赖项：\n" + string.Join('\n', missingPlugins))
                .AddCancelAction()
                .AddAction("继续", PackIconKind.ArrowRight)
                .ShowDialog();
            if (result != 1)
            {
                return;
            }
        }
        foreach (var i in resolvedPlugins)
        {
            PluginMarketService.RequestDownloadPlugin(i.Manifest.Id);
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
            var dep = PluginMarketService.ResolveMarketPlugin(i.Id);
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
        PluginMarketService.LoadPluginSource();
    }

    private void MenuItemManagePluginSources_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsPluginMarketOperationsPopupOpened = false;
        OpenDrawer("PluginSourceManageDrawer");
    }

    private void MenuItemOpenPluginsFolder_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsPluginMarketOperationsPopupOpened = false;
        Process.Start(new ProcessStartInfo()
        {
            FileName = Services.PluginService.PluginsRootPath,
            UseShellExecute = true
        });
    }

    private void ButtonBase2_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsPluginMarketOperationsPopupOpened = false;
    }

    private void ButtonAddPluginSource_OnClick(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.PluginIndexes.Add(new PluginIndexInfo());
    }

    private void ButtonRemovePluginSource_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPluginIndexInfo == null)
            return;
        SettingsService.Settings.PluginIndexes.Remove(ViewModel.SelectedPluginIndexInfo);
    }

    private void ListBoxCategory_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FindResource("PluginSource") is CollectionViewSource source)
        {
            source.View.Refresh();
        }
    }

    private void PluginSource_OnFilter(object sender, FilterEventArgs e)
    {
        if (e.Item is not KeyValuePair<string, PluginInfo> kvp) 
            return;
        var info = kvp.Value;
        if (!info.IsLocal && ViewModel.PluginCategoryIndex == 1)
        {
            e.Accepted = false;
            return;
        }
        if (!info.IsAvailableOnMarket && ViewModel.PluginCategoryIndex == 0)
        {
            e.Accepted = false;
            return;
        }
        
        var filter = ViewModel.PluginFilterText;
        if (string.IsNullOrWhiteSpace(filter))
            return;
        e.Accepted = info.Manifest.Id.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                     info.Manifest.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                     info.Manifest.Description.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private void TextBoxFilter_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        Focus();
        if (FindResource("PluginSource") is CollectionViewSource source)
        {
            source.View.Refresh();
        }
    }

    private void ButtonRestart_OnClick(object sender, RoutedEventArgs e)
    {
        RequestRestart();
    }

    private void ButtonAgreePluginNotice_OnClick(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.IsPluginMarketWarningVisible = false;
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
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            ViewModel.IsDragEntering = true;
            e.Effects = DragDropEffects.Link;
        }
        else
        {
            ViewModel.IsDragEntering = false;
            e.Effects = DragDropEffects.None;
        }
    }
    private void Grid_Drop(object sender, DragEventArgs e)
    {
        ViewModel.IsDragEntering = false;
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var fileName = ((System.Array)e.Data.GetData(DataFormats.FileDrop))?.GetValue(0)?.ToString();
            if (fileName == null)
                return;
            if (Path.GetExtension(fileName) != ".cipx")
            {
                ViewModel.MessageQueue.Enqueue($"不支持的文件：{fileName}");
                return;
            }
            try
            {
                File.Copy(fileName, Path.Combine(Services.PluginService.PluginsPkgRootPath, Path.GetFileName(fileName)), true);

                var deserializer = new DeserializerBuilder()
                    .IgnoreUnmatchedProperties()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                using var pkg = ZipFile.OpenRead(fileName);
                var mf = pkg.GetEntry("manifest.yml");
                if (mf == null)
                    return;
                var mfText = new StreamReader(mf.Open()).ReadToEnd();
                var manifest = deserializer.Deserialize<PluginManifest>(mfText);

                ViewModel.MessageQueue.Enqueue($"插件 {manifest.Name} 版本 {manifest.Version} 安装成功。");
                RequestRestart();
            }
            catch (Exception exception)
            {
                CommonDialog.ShowError($"无法安装插件：{exception.Message}");
            }
        }
    }

    private void Grid_DragLeave(object sender, DragEventArgs e)
    {
        ViewModel.IsDragEntering = false;
    }

    private void PluginsSettingsPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        PluginMarketService.RestartRequested += OnPluginMarketServiceOnRestartRequested;
    }

    private void OnPluginMarketServiceOnRestartRequested(object? sender, EventArgs args)
    {
        if (PluginMarketService.MergedPlugins.Any(x => x.Value.DownloadProgress?.IsDownloading == true))
        {
            return;
        }
        RequestRestart();
    }

    private void PluginsSettingsPage_OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        PluginMarketService.RestartRequested -= OnPluginMarketServiceOnRestartRequested;
    }

    private void ButtonOpenMarket_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.PluginCategoryIndex = 0;
    }
}