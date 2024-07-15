using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using ClassIsland.Core.Controls.CommonDialog;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Services;
using MaterialDesignThemes.Wpf;
using Microsoft.AppCenter.Crashes;
using CommonDialog = ClassIsland.Core.Controls.CommonDialog.CommonDialog;
using MessageBox = System.Windows.MessageBox;

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

    public DebugPage(SettingsService settingsService, IManagementService managementService, ConsoleService consoleService, ILessonsService lessonsService)
    {
        InitializeComponent();
        DataContext = this;
        SettingsService = settingsService;
        ManagementService = managementService;
        ConsoleService = consoleService;
        LessonsService = lessonsService;
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

    private void MenuItemDebugAppCenterCrashTest_OnClick(object sender, RoutedEventArgs e)
    {
        Crashes.GenerateTestCrash();
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
}