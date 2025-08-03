using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Dialogs.Internal;
using Avalonia.Interactivity;
using ClassIsland.Converters;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.ProfileAnalyzing;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.ViewModels.SettingsPages;
using FluentAvalonia.UI.Controls;
using Path = System.IO.Path;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// DebugPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("debug", "调试", "\ue2c8", "\ue2c7", SettingsPageCategory.Debug)]
public partial class DebugPage : SettingsPageBase
{
    private IExactTimeService ExactTimeService { get; } = App.GetService<IExactTimeService>();

    public DebugPageViewModel ViewModel { get; } = IAppHost.GetService<DebugPageViewModel>();

    public DebugPage()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void ButtonCloseDebug_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.IsDebugOptionsEnabled = false;
    }

    private void MenuItemFeatureDebugWindow_OnClick(object sender, RoutedEventArgs e)
    {
    }

    private void ButtonCrash_OnClick(object sender, RoutedEventArgs e)
    {
        throw new Exception("Crash test.");
    }

    private async void ButtonDelayCrash_OnClick(object sender, RoutedEventArgs e)
    {
        await Task.Delay(3000);
        throw new Exception("Crash test.");
    }

    private void MenuItemDebugTriggerAfterClass_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.LessonsService.DebugTriggerOnBreakingTime();
    }

    private void MenuItemDebugTriggerOnClass_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.LessonsService.DebugTriggerOnClass();
    }

    private void ButtonResetTimeSpeed_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.DebugTimeSpeed = 1;
    }

    private void MenuItemStartMainTimer_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.LessonsService.StartMainTimer();
    }

    private void MenuItemStopMainTimer_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.LessonsService.StopMainTimer();
    }

    private void MenuItemShowComponentsMigrateTips_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.ShowComponentsMigrateTip = true;
    }

    private void MenuItemShowPluginMarketWarning_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.IsPluginMarketWarningVisible = true;
    }

    private void MenuItemShowAutomationWarning_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.IsAutomationWarningVisible = true;
    }

    private async void MenuItemOverwriteSettingsVersion_OnClick(object sender, RoutedEventArgs e)
    {
        var textBox = new TextBox();
        var result = await new ContentDialog()
        {
            Title = "修改设置版本",
            Content = new StackPanel() {
                Children =
                {
                    new TextBlock()
                    {
                        Text = "输入新的设置版本。如果设置了比当前应用更低的版本，可能会触发设置迁移。"
                    },
                    textBox
                }
            },
            PrimaryButtonText = "确定",
            SecondaryButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        }.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }
        if (!Version.TryParse(textBox.Text, out var version)) 
            return;
        ViewModel.SettingsService.Settings.LastAppVersion = version;
        RequestRestart();
    }

    private async void MenuItemDumpProfileRelations_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ProfileAnalyzeService.Analyze();
        var result = ViewModel.ProfileAnalyzeService.DumpMermaidGraph();
        await File.WriteAllTextAsync(Path.Combine(CommonDirectories.AppRootFolderPath, "Profile-dump.mmd"), result);
        await CommonTaskDialogs.ShowDialog("Dump",
            $"转储成功。已保存到 {Path.GetFullPath(Path.Combine(CommonDirectories.AppRootFolderPath, "Profile-dump.mmd"))} 。");
    }
    

    private void MenuItemCrashOnTask_OnClick(object sender, RoutedEventArgs e)
    {
        Task.Run(() => throw new Exception("Crash test."));
    }

    private void MenuItemGcCollect_OnClick(object sender, RoutedEventArgs e)
    {
        var before = GC.GetTotalMemory(true);
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true);
        var after = GC.GetTotalMemory(true);
        var delta = before - after;
        if (delta <= 0)
        {
            this.ShowWarningToast("没有明显释放内存。");
            return;
        }
        this.ShowToast($"已释放 {NetworkSpeedFormater.FormatFileSize(before - after)} 内存。");
    }

    private async void MenuItemOpenMdDocs_OnClick(object sender, RoutedEventArgs e)
    {
        var textBox = new TextBox();
        var result = await new ContentDialog()
        {
            Title = "文档 Uri",
            Content = textBox,
            PrimaryButtonText = "确定",
            DefaultButton = ContentDialogButton.Primary
        }.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }
        if (!Uri.TryCreate(textBox.Text, UriKind.Absolute, out var uri))
        {
            return;
        }
        var reader = new DocumentReaderWindow()
        {
            Source = uri
        };
        reader.Show();
    }

    private void MenuItemFailFast_OnClick(object sender, RoutedEventArgs e)
    {
        Environment.FailFast("debug");
    }

    private void MenuItemCrashTestGlobal_OnClick(object sender, RoutedEventArgs e)
    {
        var thread = new Thread(() => throw new Exception());
        thread.Start();
    }

    private void MenuItemShowTestNotification_OnClick(object sender, RoutedEventArgs e)
    {
        var toastContent = new DesktopToastContent()
        {
            Title = "测试通知",
            Body = "Hello world!",
            HeroImageUri = new Uri("avares://ClassIsland/Assets/Banner.png"),
            LogoImageUri = new Uri("avares://ClassIsland/Assets/HoYoStickers/白厄_没事.png"),
            Buttons =
            {
                { "Hello!", () => { _ = CommonTaskDialogs.ShowDialog("Clicked!", "点击了按钮"); } }
            }
        };
        toastContent.Activated += (o, args) =>
        {
            _ = CommonTaskDialogs.ShowDialog("Clicked!", "点击了 Toast 通知");
        };
        PlatformServices.DesktopToastService.ShowToastAsync(toastContent);
    }

    private async void MenuItemTryGetLocation_OnClick(object sender, RoutedEventArgs e)
    {
        var location = await ViewModel.LocationService.GetLocationAsync();
        await CommonTaskDialogs.ShowDialog("你的定位", location.ToString());
    }

    private void TargetTime_OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.TargetTime = ExactTimeService.GetCurrentLocalDateTime().TimeOfDay;
        ViewModel.IsTargetTimeLoaded = true;
    }
    
    private void ButtonReset_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.DebugTimeOffsetSeconds = 0;
        ViewModel.IsTargetDateLoaded = ViewModel.IsTargetTimeLoaded = false;
        ViewModel.TargetDate = ExactTimeService.GetCurrentLocalDateTime().Date;
        ViewModel.TargetTime = ExactTimeService.GetCurrentLocalDateTime().TimeOfDay;
        ViewModel.IsTargetDateLoaded = ViewModel.IsTargetTimeLoaded = true;
    }

    private void TimePicker_OnSelectedTimeChanged(object? sender, TimePickerSelectedValueChangedEventArgs e)
    {
        if (!ViewModel.IsTargetDateTimeLoaded) return;

        DateTime now = ExactTimeService.GetCurrentLocalDateTime();
        DateTime tar = new(DateOnly.FromDateTime(now), TimeOnly.FromTimeSpan(e.NewTime ?? TimeSpan.Zero));

        ViewModel.SettingsService.Settings.DebugTimeOffsetSeconds += Math.Round((tar - now).TotalSeconds);
    }

    private void DatePicker_OnSelectedDateChanged(object? sender, SelectionChangedEventArgs selectionChangedEventArgs)
    {
        if (!ViewModel.IsTargetDateTimeLoaded) return;

        DateTime now = ExactTimeService.GetCurrentLocalDateTime().Date;
        DateTime tar = new(DateOnly.FromDateTime(ViewModel.TargetDate), TimeOnly.FromTimeSpan(now.TimeOfDay));

        ViewModel.SettingsService.Settings.DebugTimeOffsetSeconds += Math.Round((tar - now).TotalSeconds);
    }

    private void TargetDate_OnLoaded(object? sender, RoutedEventArgs e)
    {
        ViewModel.TargetDate = ExactTimeService.GetCurrentLocalDateTime().Date;
        ViewModel.IsTargetDateLoaded = true;
    }
}
