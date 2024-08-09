using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Controls.CommonDialog;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models.ProfileAnalyzing;
using ClassIsland.Services;
using ClassIsland.Shared.Interfaces;
using MaterialDesignThemes.Wpf;
using CommonDialog = ClassIsland.Core.Controls.CommonDialog.CommonDialog;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// DebugPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("debug", "调试", PackIconKind.BugOutline, PackIconKind.Bug, SettingsPageCategory.Debug)]
public partial class DebugPage : SettingsPageBase
{
    public SettingsService SettingsService { get; }

    public IManagementService ManagementService { get; }

    public ConsoleService ConsoleService { get; }

    private ILessonsService LessonsService { get; }
    public IProfileAnalyzeService ProfileAnalyzeService { get; }

    public DebugPage(SettingsService settingsService, IManagementService managementService, ConsoleService consoleService, ILessonsService lessonsService, IProfileAnalyzeService profileAnalyzeService)
    {
        InitializeComponent();
        DataContext = this;
        SettingsService = settingsService;
        ManagementService = managementService;
        ConsoleService = consoleService;
        LessonsService = lessonsService;
        ProfileAnalyzeService = profileAnalyzeService;
    }

    private void ButtonCloseDebug_OnClick(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.IsDebugOptionsEnabled = false;
    }

    private void MenuItemFeatureDebugWindow_OnClick(object sender, RoutedEventArgs e)
    {
        App.GetService<FeatureDebugWindow>().Show();
    }

    private void ButtonCrash_OnClick(object sender, RoutedEventArgs e)
    {
        throw new Exception("Crash test.");
    }

    private void ButtonDebugToastText_OnClick(object sender, RoutedEventArgs e)
    {

    }

    private void MenuItemDebugSetTime_OnClick(object sender, RoutedEventArgs e)
    {

    }

    private void MenuItemDebugTriggerAfterClass_OnClick(object sender, RoutedEventArgs e)
    {
        LessonsService.DebugTriggerOnBreakingTime();
    }

    private void MenuItemDebugTriggerOnClass_OnClick(object sender, RoutedEventArgs e)
    {
        LessonsService.DebugTriggerOnClass();
    }

    private void MenuItemTestCommonDialog_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new CommonDialog()
        {
            DialogContent = "Hello world!",
            DialogIcon = new Image()
            {
                Source = new BitmapImage(new Uri("/Assets/HoYoStickers/光辉矢愿_小喇叭.png", UriKind.Relative)),
                Width = 63,
                Height = 63
            },
            Actions = new ObservableCollection<DialogAction>()
            {
                new DialogAction()
                {
                    PackIconKind = PackIconKind.Check,
                    Name = "test"
                },
                new DialogAction()
                {
                    PackIconKind = PackIconKind.Check,
                    Name = "OK",
                    IsPrimary = true
                }
            }
        };
        dialog.ShowDialog();
        TaskDialog.ShowDialog(new WindowInteropHelper(Window.GetWindow(this)!).Handle, new TaskDialogPage()
        {
            Heading = "测试TaskDialog",
            EnableLinks = true,
            SizeToContent = false,
            Icon = TaskDialogIcon.Information,
            Expander = new TaskDialogExpander(new StackTrace().ToString()),
            Buttons = new TaskDialogButtonCollection
            {
                new TaskDialogButton("复制")
            },
            Footnote = "123123123<a href=\"https://cn.bing.com\">test link</a>",
            Text = "test task dialog."
        });
        MessageBox.Show(Window.GetWindow(this)!, dialog.ExecutedActionIndex.ToString(), "ExecutedActionIndex", MessageBoxButton.OK,
            MessageBoxImage.Information, MessageBoxResult.OK);
    }

    private async void MenuItemTestPluginIndexPack_OnClick(object sender, RoutedEventArgs e)
    {
        
    }

    private void MenuItemStartMainTimer_OnClick(object sender, RoutedEventArgs e)
    {
        LessonsService.StartMainTimer();
    }

    private void MenuItemStopMainTimer_OnClick(object sender, RoutedEventArgs e)
    {
        LessonsService.StopMainTimer();
    }

    private void MenuItemShowComponentsMigrateTips_OnClick(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.ShowComponentsMigrateTip = true;
    }

    private void MenuItemOverwriteSettingsVersion_OnClick(object sender, RoutedEventArgs e)
    {
        var r = new CommonDialogBuilder()
            .SetContent("输入新的设置版本。如果设置了比当前应用更低的版本，可能会触发设置迁移。")
            .HasInput(true)
            .AddConfirmAction()
            .ShowDialog(out var ver, Window.GetWindow(this));
        if (r != 0) return;
        if (!Version.TryParse(ver, out var version)) 
            return;
        SettingsService.Settings.LastAppVersion = version;
        RequestRestart();
    }

    private async void MenuItemDumpProfileRelations_OnClick(object sender, RoutedEventArgs e)
    {
        ProfileAnalyzeService.Analyze();
        var result = ProfileAnalyzeService.DumpMermaidGraph();
        await File.WriteAllTextAsync("./Profile-dump.mmd", result);
        CommonDialog.ShowInfo($"转储成功。已保存到 {Path.GetFullPath("./Profile-dump.mmd")} 。");
    }

    private void MenuItemFindNext_OnClick(object sender, RoutedEventArgs e)
    {
        new CommonDialogBuilder().SetContent("输入要找的元素 GUID").AddConfirmAction().HasInput(true).ShowDialog(out var guid, Window.GetWindow(this));
        new CommonDialogBuilder().SetContent("输入要找的元素索引（没有填-1）").AddConfirmAction().HasInput(true).ShowDialog(out var index, Window.GetWindow(this));
        new CommonDialogBuilder().SetContent("输入要找的设置的GUID").AddConfirmAction().HasInput(true).ShowDialog(out var settingsId, Window.GetWindow(this));
        if (int.TryParse(index, out var i))
        {
            ProfileAnalyzeService.Analyze();
            var obj = ProfileAnalyzeService.FindNextObjects(new AttachableObjectAddress(guid.ToLower(), i), settingsId);
            CommonDialog.ShowInfo(string.Join('\n', obj));
        }
    }
}