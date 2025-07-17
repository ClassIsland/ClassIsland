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
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Plugin;
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
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = "打包插件",
            FileTypeChoices = [
                IPluginService.PluginPackageFileType
            ],
            SuggestedFileName = ViewModel.SelectedPluginInfo.Manifest.Id + IPluginService.PluginPackageExtension
        });
        
        if (file == null)
            return;
        try
        {
            await Services.PluginService.PackagePluginAsync(ViewModel.SelectedPluginInfo.Manifest.Id, file.TryGetLocalPath() ?? "");
            Process.Start(new ProcessStartInfo()
            {
                FileName = Path.GetDirectoryName(file.TryGetLocalPath() ?? "") ?? "",
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

    private async void ButtonInstallFromLocal_OnClick(object sender, RoutedEventArgs e)
    {
        if (StorageProvider == null)
        {
            return;
        }
        ViewModel.IsPluginMarketOperationsPopupOpened = false;
        var file = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "从本地安装插件",
            FileTypeFilter = [IPluginService.PluginPackageFileType]
        });
        if (file.Count <= 0)
            return;
        var path = file[0].TryGetLocalPath();
        if (path == null)
        {
            return;
        }
        try
        {
            File.Copy(path, Path.Combine(Services.PluginService.PluginsPkgRootPath, Path.GetFileName(path)), true);
            RequestRestart();
        }
        catch (Exception exception)
        {
            this.ShowErrorToast($"无法安装插件", exception);
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
        ResolveDependencies(plugin, resolvedPlugins, missingPlugins);
        if (missingPlugins.Count > 0)
        {
            var result = await new ContentDialog()
            {
                Title = "缺少依赖项",
                Content = "此插件的部分必选依赖项未安装且无法从市场获取。如果继续安装此插件，此插件将可能无法工作。您要继续安装此插件吗？\n\n" +
                          "未找到的必选依赖项：\n" + string.Join('\n', missingPlugins),
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
        // TODO: 实现插件拖拽安装
        // if (e.Data.GetDataPresent(DataFormats.FileDrop))
        // {
        //     ViewModel.IsDragEntering = true;
        //     e.Effects = DragDropEffects.Link;
        // }
        // else
        // {
        //     ViewModel.IsDragEntering = false;
        //     e.Effects = DragDropEffects.None;
        // }
    }
    private void Grid_Drop(object sender, DragEventArgs e)
    {
        // TODO: 实现插件拖拽安装
        // ViewModel.IsDragEntering = false;
        // if (e.Data.GetDataPresent(DataFormats.FileDrop))
        // {
        //     var fileName = ((System.Array)e.Data.GetData(DataFormats.FileDrop))?.GetValue(0)?.ToString();
        //     if (fileName == null)
        //         return;
        //     if (Path.GetExtension(fileName) != ".cipx")
        //     {
        //         ViewModel.MessageQueue.Enqueue($"不支持的文件：{fileName}");
        //         return;
        //     }
        //     try
        //     {
        //         File.Copy(fileName, Path.Combine(Services.PluginService.PluginsPkgRootPath, Path.GetFileName(fileName)), true);
        //
        //         var deserializer = new DeserializerBuilder()
        //             .IgnoreUnmatchedProperties()
        //             .WithNamingConvention(CamelCaseNamingConvention.Instance)
        //             .Build();
        //         using var pkg = ZipFile.OpenRead(fileName);
        //         var mf = pkg.GetEntry("manifest.yml");
        //         if (mf == null)
        //             return;
        //         var mfText = new StreamReader(mf.Open()).ReadToEnd();
        //         var manifest = deserializer.Deserialize<PluginManifest>(mfText);
        //
        //         ViewModel.MessageQueue.Enqueue($"插件 {manifest.Name} 版本 {manifest.Version} 安装成功。");
        //         RequestRestart();
        //     }
        //     catch (Exception exception)
        //     {
        //         CommonDialog.ShowError($"无法安装插件：{exception.Message}");
        //     }
        // }
    }

    private void Grid_DragLeave(object sender, DragEventArgs e)
    {
        ViewModel.IsDragEntering = false;
    }

    private void PluginsSettingsPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        ViewModel.PluginMarketService.RestartRequested += OnPluginMarketServiceOnRestartRequested;
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
}

