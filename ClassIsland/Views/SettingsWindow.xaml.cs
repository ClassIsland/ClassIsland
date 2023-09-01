using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ClassIsland.Controls;
using ClassIsland.Enums;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.ViewModels;
using MaterialDesignThemes.Wpf;
using MdXaml;
using Microsoft.Toolkit.Uwp.Notifications;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

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

    public SettingsWindow()
    {
        UpdateService = App.GetService<UpdateService>();
        TaskBarIconService = App.GetService<TaskBarIconService>();
        WallpaperPickingService = App.GetService<WallpaperPickingService>();
        MiniInfoProviderHostService = App.GetService<MiniInfoProviderHostService>();
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
        if (e.PropertyName == nameof(Settings.LastCheckUpdateInfoCache))
        {
            UpdateCache();
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
        var fd = e.Transform(Settings.LastCheckUpdateInfoCache.ReleaseNotes);
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

    private void ButtonContributors_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("ContributorsDrawer");
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
        var screenShot = WallpaperPickingService.GetScreenShot("Progman");
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
}
