using System;
using System.Collections.Generic;
using System.ComponentModel;
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

    public PluginsSettingsPage(IPluginService pluginService)
    {
        InitializeComponent();
        DataContext = this;
        PluginService = pluginService;
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    private async Task UpdateReadmeDocument()
    {
        var path = System.IO.Path.Combine(ViewModel.SelectedPluginManifest.PluginFolderPath,
            ViewModel.SelectedPluginManifest.Readme);
        if (!File.Exists(path))
        {
            return;
        }

        var md = await File.ReadAllTextAsync(path);
        ViewModel.ReadmeDocument = MarkdownConvertHelper.ConvertMarkdown(md);
    }

    private async void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ViewModel.SelectedPluginManifest):
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
}