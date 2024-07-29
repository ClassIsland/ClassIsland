using System;
using System.Collections.Generic;
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
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Controls.CommonDialog;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Helpers;
using ClassIsland.Models.AllContributors;
using ClassIsland.Services;
using ClassIsland.Services.Management;
using ClassIsland.ViewModels.SettingsPages;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// AboutSettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("about", "关于 ClassIsland", PackIconKind.InfoCircleOutline, PackIconKind.InfoCircle, SettingsPageCategory.About)]
public partial class AboutSettingsPage : SettingsPageBase
{
    public AboutSettingsViewModel ViewModel { get; } = new();

    public IManagementService ManagementService { get; }

    public SettingsService SettingsService { get; }

    public DiagnosticService DiagnosticService { get; }

    public AboutSettingsPage(IManagementService managementService, SettingsService settingsService, DiagnosticService diagnosticService)
    {
        DataContext = this;
        ManagementService = managementService;
        SettingsService = settingsService;
        DiagnosticService = diagnosticService;
        InitializeComponent();
        var r = new StreamReader(Application.GetResourceStream(new Uri("/Assets/LICENSE.txt", UriKind.Relative))!.Stream);
        ViewModel.License = r.ReadToEnd();
    }

    private void AppIcon_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel.AppIconClickCount++;
        if (ViewModel.AppIconClickCount >= 10)
        {
            if (ManagementService.Policy.DisableDebugMenu)
            {
                CommonDialog.ShowError("调试菜单已被您的组织禁用。");
                return;
            }
            SettingsService.Settings.IsDebugOptionsEnabled = true;
        }
    }

    private void ButtonGithub_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = "https://github.com/HelloWRC/ClassIsland",
            UseShellExecute = true
        });
    }

    private void ButtonFeedback_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = "https://github.com/HelloWRC/ClassIsland/issues",
            UseShellExecute = true
        });
    }

    private void Hyperlink2_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = "https://github.com/DuguSand/class_form",
            UseShellExecute = true
        });
    }

    private void ButtonDiagnosticInfo_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.DiagnosticInfo = DiagnosticService.GetDiagnosticInfo();
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

    private async void ButtonContributors_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("ContributorsDrawer");
        await RefreshContributors();
    }

    private void ButtonThirdPartyLibs_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("ThirdPartyLibs");
    }

    private async Task RefreshContributors()
    {
        ViewModel.IsRefreshingContributors = true;
        try
        {
            SettingsService.Settings.ContributorsCache =
                await WebRequestHelper.GetJson<AllContributorsRc>(new Uri(
                    "https://mirror.ghproxy.com/?q=https%3A%2F%2Fraw.githubusercontent.com%2FClassIsland%2FClassIsland%2Fmaster%2F.all-contributorsrc"));
        }
        catch (Exception ex)
        {
            App.GetService<ILogger<AboutSettingsPage>>().LogError(ex, "无法获取贡献者名单。");
        }
        ViewModel.IsRefreshingContributors = false;
    }

    private async void ButtonRefreshContributors_OnClick(object sender, RoutedEventArgs e)
    {
        await RefreshContributors();
    }

    private void ButtonPrivacy_OnClick(object sender, RoutedEventArgs e)
    {
        new DocumentReaderWindow()
        {
            Source = new Uri("/Assets/Documents/Privacy.md", UriKind.RelativeOrAbsolute),
            Owner = Window.GetWindow(this),
            Title = "ClassIsland 隐私政策"
        }.ShowDialog();
    }
}