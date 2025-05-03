using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Controls.CommonDialog;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Helpers;
using ClassIsland.Models.AllContributors;
using ClassIsland.Services;
using ClassIsland.ViewModels.SettingsPages;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using Sentry;

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
        if (ViewModel.AppIconClickCount >= 10 && !SettingsService.Settings.IsDebugOptionsEnabled)
        {
            if (ManagementService.Policy.DisableDebugMenu)
            {
                CommonDialog.ShowError("调试菜单已被您的组织禁用。");
                return;
            }
#if !DEBUG
            var r1 = new CommonDialogBuilder()
                .SetPackIcon(PackIconKind.Bug)
                .SetCaption("启用调试菜单")
                .SetContent(
                    "您正在发布版本的 ClassIsland 中启用仅供开发使用的调试菜单。请注意此功能仅限于开发和调试用途，ClassIsland 开发者不对以非开发用途使用此页面中功能造成的任何后果负责，也不接受以非开发用途使用时产生的 Bug 的反馈。\n\n如果您确实要启用此功能，请在下方文本框输入⌈我已知晓并同意，开发者不对以非开发用途使用此页面功能造成的任何后果负责，也不接受以非开发用途使用此页面功能产生的 Bug 的反馈⌋，然后点击【继续】。")
                .HasInput(true)
                .AddCancelAction()
                .AddAction("继续", PackIconKind.ArrowRight, true)
                .ShowDialog(out var confirm, Window.GetWindow(this));
            if (r1 != 1 || confirm != "我已知晓并同意，开发者不对以非开发用途使用此页面功能造成的任何后果负责，也不接受以非开发用途使用此页面功能产生的 Bug 的反馈")
            {
                return;
            }
#endif
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
                    "https://github.moeyy.xyz/https://raw.githubusercontent.com/ClassIsland/ClassIsland/master/.all-contributorsrc"));
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
            Source = new Uri("/Assets/Documents/Privacy_.md", UriKind.RelativeOrAbsolute),
            Owner = Window.GetWindow(this),
            Title = "ClassIsland 隐私政策"
        }.ShowDialog();
    }

    private async void Sayings_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.IsSayingBusy)
        {
            return;
        }

        if (ViewModel.SayingsCollection.Count <= 0)
        {
            var stream = Application.GetResourceStream(new Uri("/Assets/Tellings.txt", UriKind.Relative))?.Stream;
            if (stream == null)
            {
                return;
            }

            var sayings = await new StreamReader(stream).ReadToEndAsync();
            var collection = new ObservableCollection<string>(sayings.Split("\r\n"));
            var countRaw = collection.Count;
            for (var i = 0; i < countRaw; i++)
            {
                var randomIndex = ViewModel.Random.Next(0, collection.Count - 1);
                ViewModel.SayingsCollection.Add(collection[randomIndex]);
                collection.RemoveAt(randomIndex);
            }
        }
        //Console.WriteLine(ViewModel.SayingsCollection.Count);
        if (ViewModel.SayingsCollection.Count > 0)
        {
            ViewModel.Sayings = ViewModel.SayingsCollection[0];
            ViewModel.SayingsCollection.RemoveAt(0);
        }
        SentrySdk.Metrics.Increment("views.settings.about.sayings.click");
    }

    private void UIElementAppInfo_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel.AppIconClickCount++;
        if (ViewModel.AppIconClickCount >= 20 && !ManagementService.Policy.DisableEasterEggs)
        {
            Clipboard.SetDataObject("5oS/5oiR5Lus5Zyo6YKj6bKc6Iqx6Iqs6Iqz55qE6KW/6aOO5bC95aS06YeN6YCi44CC", false);
        }
    }
}