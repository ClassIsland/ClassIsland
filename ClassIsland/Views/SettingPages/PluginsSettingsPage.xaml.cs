using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Services;
using ClassIsland.ViewModels.SettingsPages;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
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

    public PluginsSettingsPage(IPluginService pluginService, IPluginMarketService pluginMarketService, SettingsService settingsService)
    {
        InitializeComponent();
        DataContext = this;
        PluginService = pluginService;
        PluginMarketService = pluginMarketService;
        SettingsService = settingsService;
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        PluginMarketService.RestartRequested += (sender, args) => RequestRestart();
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
        if (!File.Exists(path))
        {
            ViewModel.ReadmeDocument = new FlowDocument();
            return;
        }

        var md = await File.ReadAllTextAsync(path);
        ViewModel.ReadmeDocument = MarkdownConvertHelper.ConvertMarkdown(md);
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
        PluginMarketService.RequestDownloadPlugin(ViewModel.SelectedPluginInfo.Manifest.Id);
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
        e.Accepted = info.Manifest.Id.Contains(filter) ||
                     info.Manifest.Name.Contains(filter) ||
                     info.Manifest.Description.Contains(filter);
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
}