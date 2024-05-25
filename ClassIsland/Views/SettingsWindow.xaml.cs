using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

using ClassIsland.Controls;
using ClassIsland.Controls.NotificationProviders;
using ClassIsland.Core.Abstraction.Services;
using ClassIsland.Core.Enums;
using ClassIsland.Helpers;
using ClassIsland.Models;
using ClassIsland.Models.AllContributors;
using ClassIsland.Models.Weather;
using ClassIsland.Services;
using ClassIsland.Services.Management;
using ClassIsland.ViewModels;

using MaterialDesignThemes.Wpf;

using MdXaml;

using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Logging;

using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using CommonDialog = ClassIsland.Controls.CommonDialog;
using FontFamily = System.Windows.Media.FontFamily;
using Image = System.Windows.Controls.Image;
using ListBox = System.Windows.Controls.ListBox;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace ClassIsland.Views;
/// <summary>
/// SettingsWindow.xaml 的交互逻辑
/// </summary>
public partial class SettingsWindow : MyWindow
{
    public SettingsViewModel ViewModel
    {
        get;
        set;
    } = new();

    public MainViewModel MainViewModel
    {
        get;
        set;
    } = new();

    public Settings Settings
    {
        get;
        set;
    } = new();

    public bool IsOpened
    {
        get;
        set;
    } = false;
    
    public UpdateService UpdateService
    {
        get;
    }

    public WallpaperPickingService WallpaperPickingService
    {
        get;
    }

    private TaskBarIconService TaskBarIconService
    {
        get;
    }

    public MiniInfoProviderHostService MiniInfoProviderHostService
    {
        get;
    }

    public ManagementService ManagementService { get; }

    private List<byte[]> _testMemoryLeakList = new();

    public DiagnosticService DiagnosticService { get; }

    public WeatherService WeatherService { get; } = App.GetService<WeatherService>();

    public ExactTimeService ExactTimeService { get; } = App.GetService<ExactTimeService>();

    public SettingsWindow()
    {
        UpdateService = App.GetService<UpdateService>();
        TaskBarIconService = App.GetService<TaskBarIconService>();
        WallpaperPickingService = App.GetService<WallpaperPickingService>();
        MiniInfoProviderHostService = App.GetService<MiniInfoProviderHostService>();
        DiagnosticService = App.GetService<DiagnosticService>();
        ManagementService = App.GetService<ManagementService>();
        InitializeComponent();
        DataContext = this;
        var settingsService = App.GetService<SettingsService>();
        settingsService.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == "Settings")
            {
                settingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
            }
        };
        var style = (Style)FindResource("NotificationsListBoxItemStyle");
        //style.Setters.Add(new EventSetter(ListBoxItem.MouseDoubleClickEvent, new System.Windows.Input.MouseEventHandler(EventSetter_OnHandler)));
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Settings.UpdateReleaseInfo):
                UpdateCache();
                break;
            case nameof(Settings.SpeechSource):
                RequireRestart();
                break;
        }

        RefreshDescription();
    }

    private void UpdateCache()
    {
        var e = new Markdown()
        {
            Heading1Style = (Style)FindResource("MarkdownHeadline1Style"),
            Heading2Style = (Style)FindResource("MarkdownHeadline2Style"),
            Heading3Style = (Style)FindResource("MarkdownHeadline3Style"),
            Heading4Style = (Style)FindResource("MarkdownHeadline4Style"),
            //CodeBlockStyle = (Style)FindResource("MarkdownCodeBlockStyle"),
            //NoteStyle = (Style)FindResource("MarkdownNoteStyle"),
            ImageStyle = (Style)FindResource("MarkdownImageStyle"),
        };
        var fd = e.Transform(Settings.UpdateReleaseInfo);
        fd.FontFamily = (FontFamily)FindResource("HarmonyOsSans");
        ViewModel.CurrentMarkdownDocument = fd;
    }

    protected override void OnInitialized(EventArgs e)
    {
        RefreshMonitors();
        var r = new StreamReader(Application.GetResourceStream(new Uri("/Assets/LICENSE.txt", UriKind.Relative))!.Stream);
        ViewModel.License = r.ReadToEnd();
        base.OnInitialized(e);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        Settings.PropertyChanged += SettingsOnPropertyChanged;
        UpdateCache();
        RefreshDescription();
        ViewModel.CitySearchResults = WeatherService.GetCitiesByName("");
        base.OnContentRendered(e);
    }

    private void RefreshDescription()
    {
        var m = from c in UpdateService.UpdateChannels where c.RootUrl == Settings.SelectedChannel select c;
        ViewModel.SelectedChannelModel = m.ToList().Count > 0 ? m.ToList()[0] : new UpdateChannel();
    }

    private void RefreshMonitors()
    {
        ViewModel.Screens = Screen.AllScreens;
    }

    private void ButtonRefreshMonitors_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshMonitors();
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

    private void SettingsWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
        IsOpened = false;
    }

    private void ButtonCrash_OnClick(object sender, RoutedEventArgs e)
    {
        throw new Exception("Crash test.");
    }

    private void HyperlinkMsAppCenter_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = "https://learn.microsoft.com/zh-cn/appcenter/sdk/data-collected",
            UseShellExecute = true
        });
    }

    private void MyDrawerHost_OnDrawerClosing(object? sender, DrawerClosingEventArgs e)
    {
    }

    private async void ButtonCheckUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        await UpdateService.CheckUpdateAsync();
    }

    private void ButtonDebugResetUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        Settings.LastUpdateStatus = UpdateStatus.UpToDate;
    }

    private void ButtonDebugDownloaded_OnClick(object sender, RoutedEventArgs e)
    {
        Settings.LastUpdateStatus = UpdateStatus.UpdateDownloaded;
    }

    private async void ButtonStartDownloading_OnClick(object sender, RoutedEventArgs e)
    {
        await UpdateService.DownloadUpdateAsync();
    }

    private async void ButtonRestartToUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(".\\UpdateTemp\\update.zip"))
            return;
        await UpdateService.RestartAppToUpdateAsync();
    }

    private void ButtonDebugToastText_OnClick(object sender, RoutedEventArgs e)
    {
        
    }

    private void ButtonCancelUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        if (UpdateService.CurrentWorkingStatus == UpdateWorkingStatus.DownloadingUpdates)
        {
            UpdateService.StopDownloading();
        }

        if (Settings.LastUpdateStatus == UpdateStatus.UpdateDownloaded)
        {
            UpdateService.RemoveDownloadedFiles();
        }
    }

    private void ButtonDebugNetworkError_OnClick(object sender, RoutedEventArgs e)
    {
        //UpdateService.CurrentWorkingStatus = UpdateWorkingStatus.NetworkError;
    }

    private void UpdateErrorMessage_OnActionClick(object sender, RoutedEventArgs e)
    {
        UpdateService.NetworkErrorException = null;
    }

    private void ButtonAdvancedSettings_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("AdvancedUpdateSettings");
    }

    private void OpenDrawer(string key)
    {
        MyDrawerHost.IsRightDrawerOpen = true;
        ViewModel.DrawerContent = FindResource(key);
    }

    private async void ButtonForceUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        MyDrawerHost.IsRightDrawerOpen = false;
        await UpdateService.CheckUpdateAsync(isForce:true);
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
            Settings.IsDebugOptionsEnabled = true;
        }
    }

    private void ButtonCloseDebug_OnClick(object sender, RoutedEventArgs e)
    {
        Settings.IsDebugOptionsEnabled = false;
        ViewModel.AppIconClickCount = 0;
    }

    private void MenuItemDebugScreenShot_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog()
        {
            Title = "打开测试取色的文件。"
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
        {
            return;
        }
        
        var screenShot = new Bitmap(System.Drawing.Image.FromFile(dialog.FileName));
        ViewModel.TestImage = BitmapConveters.ConvertToBitmapImage(screenShot);
        screenShot.Save("./desktop.png");
        var w = new Stopwatch();
        w.Start();
        var r = ColorOctTreeNode.ProcessImage(screenShot)
            .OrderByDescending(i=>
            {
                var c = (Color)ColorConverter.ConvertFromString(i.Key);
                WallpaperPickingService.ColorToHsv(c, out var h, out var s, out var v);
                return (s + v * (- (v - 0.1) * (v - 1.1) * 4)) * Math.Log2(i.Value);
            })
            .ThenByDescending(i => i.Value)
            .ToList();
        ViewModel.DebugImageAccentColors.Clear();
        ViewModel.DebugOutputs = "";
        ViewModel.DebugOutputs += $"主题色提取用了{w.ElapsedMilliseconds}ms.\n";
        ViewModel.DebugOutputs += $"主题色：\n";
        for (var i = 0; i < Math.Min(r.Count, 15); i++)
        {
            ViewModel.DebugOutputs += $"{r[i].Key} - {r[i].Value}\n";
            ViewModel.DebugImageAccentColors.Add((Color)ColorConverter.ConvertFromString(r[i].Key));
        }
        //Debugger.Break();
    }

    private async void ButtonUpdateWallpaper_OnClick(object sender, RoutedEventArgs e)
    {
        await WallpaperPickingService.GetWallpaperAsync();
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.IsNotificationSettingsPanelOpened = true;
    }

    private void Selector_OnSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
        {
            ViewModel.IsNotificationSettingsPanelOpened = false;
            return;
        }
        ViewModel.IsNotificationSettingsPanelOpened = true;

    }

    private void EventSetter_OnHandler(object sender, MouseEventArgs e)
    {
        if (ViewModel.NotificationSettingsSelectedProvider == null)
        {
            ViewModel.IsNotificationSettingsPanelOpened = false;
            return;
        }
        ViewModel.IsNotificationSettingsPanelOpened = true;
    }

    private async void ButtonBrowseWindows_OnClick(object sender, RoutedEventArgs e)
    {
        var w = new WindowsPicker(Settings.WallpaperClassName)
        {
            Owner = this,
        };
        var r = w.ShowDialog();
        Settings.WallpaperClassName = w.SelectedResult ?? "";
        if (r == true)
        {
            await WallpaperPickingService.GetWallpaperAsync();
        }
        GC.Collect();
    }

    private void MenuItemExperimentalSettings_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsPopupMenuOpened = false;
        OpenDrawer("ExperimentalSettings");
    }

    private async void ButtonRefreshWeather_OnClick(object sender, RoutedEventArgs e)
    {
        await WeatherService.QueryWeatherAsync();
    }

    private void ButtonEditCurrentCity_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("CitySearcher");

    }

    private void TextBoxSearchCity_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.CitySearchResults = WeatherService.GetCitiesByName(((TextBox)sender).Text);
    }

    private async void SelectorCity_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var listbox = (ListBox)sender;
        var city = (City?)listbox.SelectedItem;
        if (city == null)
        {
            e.Handled = true;
            //Settings.CityName = "";
            return;
        }
        Settings.CityName = city.Name;
        await WeatherService.QueryWeatherAsync();
    }

    private void TextBoxSearchCity_OnFocusableChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        ((UIElement)sender).Focus();
    }

    private void MenuItemTestWeatherNotificationControl_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.WeatherNotificationControlTest =
            new WeatherForecastNotificationProvider(false, Settings.LastWeatherInfo);
    }

    private void MenuItemDebugTriggerAfterClass_OnClick(object sender, RoutedEventArgs e)
    {
        App.GetService<NotificationHostService>().OnOnBreakingTime(this, EventArgs.Empty);
    }

    private void MenuItemDebugTriggerOnClass_OnClick(object sender, RoutedEventArgs e)
    {
        App.GetService<NotificationHostService>().OnOnClass(this, EventArgs.Empty);
    }

    private void ButtonPreviewWallpaper_OnClick(object sender, RoutedEventArgs e)
    {
        var w = App.GetService<WallpaperPreviewWindow>();
        w.Owner = this;
        w.ShowDialog();
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

    private void MenuItemDebugSplashWindow_OnClick(object sender, RoutedEventArgs e)
    {
        App.GetService<SplashWindow>().Show();
    }

    private void MenuItemDebugConsole_OnChecked(object sender, RoutedEventArgs e)
    {
        ConsoleService.ConsoleVisible = true;
    }

    private void MenuItemDebugConsole_OnUnchecked(object sender, RoutedEventArgs e)
    {
        ConsoleService.ConsoleVisible = false;
    }

    private async void MenuItemTestUpdateMirrors_OnClick(object sender, RoutedEventArgs e)
    {
        await App.GetService<UpdateNodeSpeedTestingService>().RunSpeedTestAsync();
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
        TaskDialog.ShowDialog(new WindowInteropHelper(this).Handle, new TaskDialogPage()
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
        MessageBox.Show(this, dialog.ExecutedActionIndex.ToString(), "ExecutedActionIndex", MessageBoxButton.OK,
            MessageBoxImage.Information, MessageBoxResult.OK);
    }

    public void OpenUri(Uri uri)
    {
        if (uri.Segments.Length <= 2)
            return;
        var uriSegment = uri.Segments[2].EndsWith('/') ? uri.Segments[2][..^1] : uri.Segments[2] ;
        RootTabControl.SelectedIndex = uriSegment switch
        {
            "general" => 0,
            "appearance" => 1,
            "notification" => 2,
            "window" => 3,
            "weather" => 4,
            "update" => 5,
            "privacy" => 6,
            "about" => 7,
            "debug" => 8,
            "debug_brushes" => 9,
            _ => RootTabControl.SelectedIndex
        };
        if (uri.Segments.Length <= 3)
            return;
        switch (uriSegment)
        {
            case "notification":
                ViewModel.NotificationSettingsSelectedProvider = uri.Segments[3].ToLower();
                break;
        }
    }

    private void ButtonDiagnosticInfo_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.DiagnosticInfo = DiagnosticService.GetDiagnosticInfo();
    }

    private async void MenuItemMemoryLeakTest_OnClick(object sender, RoutedEventArgs e)
    {
        
    }

    private void MenuItemConnetManagemnt_OnClick(object sender, RoutedEventArgs e)
    {
    }

    private void ButtonOpenSpeechSettings_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(@"C:\WINDOWS\system32\rundll32.exe", @"shell32.dll,Control_RunDLL C:\WINDOWS\system32\Speech\SpeechUX\sapi.cpl");
    }

    private void ButtonChangelogs_OnClick(object sender, RoutedEventArgs e)
    {
        App.GetService<MainWindow>().OpenHelpsWindow();
        App.GetService<HelpsWindow>().InitDocumentName = "新增功能";
        App.GetService<HelpsWindow>().ViewModel.SelectedDocumentName = "新增功能";
    }

    private void ButtonTestSpeeching_OnClick(object sender, RoutedEventArgs e)
    {
        App.GetService<ISpeechService>().ClearSpeechQueue();
        App.GetService<ISpeechService>().EnqueueSpeechQueue(ViewModel.TestSpeechText);
    }

    private void RequireRestart()
    {
        ViewModel.IsRestartRequired = true;
        ShowRestartDialog();
    }

    private async void ShowRestartDialog()
    {
        var r = await DialogHost.Show(FindResource("RestartDialog"), "SettingsWindow");
        if (r as bool? != true)
            return;
        App.Restart();
    }

    private void ButtonRestartChip_OnClick(object sender, RoutedEventArgs e)
    {
        ShowRestartDialog();
    }

    private async void ButtonSyncTimeNow_OnClick(object sender, RoutedEventArgs e)
    {
        await Task.Run(() =>
        {
            ExactTimeService.Sync();
        });
    }

    private void MenuItemFeatureDebugWindow_OnClick(object sender, RoutedEventArgs e)
    {
        App.GetService<FeatureDebugWindow>().Show();
    }

    private void MenuItemDebugSetTime_OnClick(object sender, RoutedEventArgs e)
    {

    }

    private void MenuItemDebugAppCenterCrashTest_OnClick(object sender, RoutedEventArgs e)
    {
        Crashes.GenerateTestCrash();
    }

    private void MenuItemAppLogs_OnClick(object sender, RoutedEventArgs e)
    {
        App.GetService<AppLogsWindow>().Open();
    }

    private async void ButtonRefreshContributors_OnClick(object sender, RoutedEventArgs e)
    {
        await RefreshContributors();
    }

    private async Task RefreshContributors()
    {
        ViewModel.IsRefreshingContributors = true;
        try
        {
            Settings.ContributorsCache =
                await WebRequestHelper.GetJson<AllContributorsRc>(new Uri(
                    "https://mirror.ghproxy.com/?q=https%3A%2F%2Fraw.githubusercontent.com%2FHelloWRC%2FClassIsland%2Fmaster%2F.all-contributorsrc"));
        }
        catch (Exception ex)
        {
            App.GetService<ILogger<SettingsWindow>>().LogError(ex, "无法获取贡献者名单。");
        }
        ViewModel.IsRefreshingContributors = false;
    }

    private async void MenuItemExitManagement_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await ManagementService.ExitManagementAsync();
        }
        catch (Exception ex)
        {
            App.GetService<ILogger<SettingsWindow>>().LogError(ex, "无法退出管理。");
            CommonDialog.ShowError($"无法退出管理：{ex.Message}");
        }
    }

    private async void MenuItemExportDiagnosticInfo_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var r = CommonDialog.ShowDialog("ClassIsland", $"您正在导出应用的诊断数据。导出的诊断数据将包含应用当前运行的日志、系统及环境信息、应用设置、当前加载的档案和集控设置（如有），可能包含敏感信息，请在导出后注意检查。", new BitmapImage(new Uri("/Assets/HoYoStickers/帕姆_注意.png", UriKind.Relative)),
                60, 60, [
                    new DialogAction()
                    {
                        PackIconKind = PackIconKind.Cancel,
                        Name = "取消"
                    },
                    new DialogAction()
                    {
                        PackIconKind = PackIconKind.Check,
                        Name = "继续",
                        IsPrimary = true
                    }
                ]);
            if (r != 1)
                return;
            var dialog = new SaveFileDialog()
            {
                Title = "导出诊断数据",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Filter = "压缩文件(*.zip)|*.zip"
            };
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            await DiagnosticService.ExportDiagnosticData(dialog.FileName);
        }
        catch (Exception exception)
        {
            CommonDialog.ShowError($"导出失败：{exception.Message}");
        }
    }

    private void CollectionViewSourceNotificationProviders_OnFilter(object sender, FilterEventArgs e)
    {
        var i = e.Item as string;
        if (i == null)
            return;
        var host = App.GetService<NotificationHostService>();
        e.Accepted = host.NotificationProviders.FirstOrDefault(x => x.ProviderGuid.ToString() == i) != null;
    }
}
