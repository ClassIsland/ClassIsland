using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.UI;
using ClassIsland.Helpers;
using ClassIsland.Models.AllContributors;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.ViewModels.SettingsPages;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using Sentry;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// AboutSettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("about", "关于 ClassIsland", "\ue9e4", "\ue9e3", SettingsPageCategory.About)]
public partial class AboutSettingsPage : SettingsPageBase
{
    public AboutSettingsViewModel ViewModel { get; } = IAppHost.GetService<AboutSettingsViewModel>();

    public AboutSettingsPage()
    {
        DataContext = this;
        InitializeComponent();
        var r = new StreamReader(AssetLoader.Open(new Uri("avares://ClassIsland/Assets/LICENSE.txt")));
        ViewModel.License = r.ReadToEnd();
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

    private async void ButtonDiagnosticInfo_OnClick(object sender, RoutedEventArgs e)
    {
        var diagInfo = ViewModel.DiagnosticService.GetDiagnosticInfo();
        var dialog = new ContentDialog()
        {
            Title = "诊断信息",
            Content = new TextBox()
            {
                Text = diagInfo
            },
            IsSecondaryButtonEnabled = true,
            PrimaryButtonText = "确定",
            SecondaryButtonText = "复制",
            DefaultButton = ContentDialogButton.Primary
        };
        dialog.SecondaryButtonClick += ButtonCopyDiagnosticInfo_OnClick;
        await dialog.ShowAsync();
    }

    private async void ButtonCopyDiagnosticInfo_OnClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        bool success = false;
        try
        {
            await TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(ViewModel.DiagnosticService.GetDiagnosticInfo());
            success = true;
        }
        catch (Exception ex)
        {
            App.GetService<ILogger<AboutSettingsPage>>().LogError(ex, "复制诊断信息失败。");
            ToastsHelper.ShowErrorToast(this, "复制失败，请全选诊断信息文本后手动复制。");
        }
        if (success)
        {
            ToastsHelper.ShowSuccessToast(this, "复制成功！");
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
            ViewModel.SettingsService.Settings.ContributorsCache =
                await WebRequestHelper.GetJson<AllContributorsRc>(new Uri(
                    "https://ghfast.top/https://raw.githubusercontent.com/ClassIsland/ClassIsland/master/.all-contributorsrc"));
        }
        catch (Exception ex)
        {
            App.GetService<ILogger<AboutSettingsPage>>().LogError(ex, "无法获取贡献者名单。");
            this.ShowToast(new ToastMessage()
            {
                Severity = InfoBarSeverity.Error,
                Title = "无法获取贡献者名单",
                Message = ex.Message,
                AutoClose = false
            });
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
            Source = new Uri("avares://ClassIsland/Assets/Documents/Privacy_.md"),
            Title = "ClassIsland 隐私政策"
        }.ShowDialog((TopLevel.GetTopLevel(this) as Window)!);
    }

    private async void Sayings_OnMouseLeftButtonDown(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsSayingBusy)
        {
            return;
        }

        if (ViewModel.SayingsCollection.Count <= 0)
        {
            var stream = AssetLoader.Open(new Uri("avares://ClassIsland/Assets/Tellings.txt"));

            var sayings = await new StreamReader(stream).ReadToEndAsync();
            string[] sayingsArray;
            if (sayings.Contains("\r\n"))
            {
                sayingsArray = sayings.Split("\r\n");
            }
            else
            {
                sayingsArray = sayings.Split("\n");
            }
            var collection = new ObservableCollection<string>(sayingsArray);
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

    private void UIElementAppInfo_OnMouseDown(object? sender, RoutedEventArgs pointerPressedEventArgs)
    {
        ViewModel.AppIconClickCount++;
        if (ViewModel.AppIconClickCount >= 20 && !ViewModel.ManagementService.Policy.DisableEasterEggs)
        {
            TopLevel.GetTopLevel(this)?.Clipboard?
                .SetTextAsync("5oS/5oiR5Lus5Zyo6YKj6bKc6Iqx6Iqs6Iqz55qE6KW/6aOO5bC95aS06YeN6YCi44CC");
        }
    }

    private async void SettingsExpanderItemShowOssLicense_OnClick(object? sender, RoutedEventArgs e)
    {
        var license = await new StreamReader(AssetLoader.Open(new Uri("avares://ClassIsland/Assets/LICENSE.txt")))
            .ReadToEndAsync();
        await new ContentDialog()
        {
            Title = "开放源代码许可",
            Content = new TextBlock()
            {
                Text = license
            },
            PrimaryButtonText = "关闭",
            DefaultButton = ContentDialogButton.Primary
        }.ShowAsync();
    }

    private async void DebugBorder_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ViewModel.AppIconClickCount++;
        if (ViewModel.SettingsService.Settings.IsDebugOptionsEnabled)
        {
            this.ShowToast("您已启用调试菜单，无需继续操作。");
            return;
        }
        if (ViewModel.AppIconClickCount >= 10)
        {
            if (ViewModel.ManagementService.Policy.DisableDebugMenu)
            {
                _ = CommonTaskDialogs.ShowDialog("调试菜单已禁用", "您的组织禁用了调试菜单。", this);
                return;
            }

#if !DEBUG
            var textBox = new TextBox();
            var r = await new ContentDialog()
            {
                Title = "启用调试菜单",
                Content = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock()
                        {
                            TextWrapping = TextWrapping.Wrap,
                            Text =
                                "您正在发布版本的 ClassIsland 中启用仅供开发使用的调试菜单。请注意此功能仅限于开发和调试用途，ClassIsland 开发者不对以非开发用途使用此页面中功能造成的任何后果负责，也不接受以非开发用途使用时产生的 Bug 的反馈。\n\n如果您确实要启用此功能，请在下方文本框输入⌈我已知晓并同意，开发者不对以非开发用途使用此页面功能造成的任何后果负责，也不接受以非开发用途使用此页面功能产生的 Bug 的反馈⌋，然后点击【继续】。"
                        },
                        textBox
                    }
                },
                PrimaryButtonText = "继续",
                SecondaryButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            }.ShowAsync();
            
            
            if (r != ContentDialogResult.Primary)
            {
                return;
            }

            if (textBox.Text != "我已知晓并同意，开发者不对以非开发用途使用此页面功能造成的任何后果负责，也不接受以非开发用途使用此页面功能产生的 Bug 的反馈")
            {
                this.ShowWarningToast("验证结果不正确，请重新输入。");
                return;
            }
#endif
            ViewModel.SettingsService.Settings.IsDebugOptionsEnabled = true;
            this.ShowSuccessToast("已启用调试菜单。");
        }
    }
}

