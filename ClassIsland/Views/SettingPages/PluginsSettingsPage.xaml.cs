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

    public PluginsSettingsPage(IPluginService pluginService, IPluginMarketService pluginMarketService)
    {
        InitializeComponent();
        DataContext = this;
        PluginService = pluginService;
        PluginMarketService = pluginMarketService;
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        PluginMarketService.RestartRequested += (sender, args) => RequestRestart();
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
    }

    private void ButtonInstallPlugin_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedPluginInfo == null)
            return;
        PluginMarketService.RequestDownloadPlugin(ViewModel.SelectedPluginInfo.Manifest.Id);
    }
}