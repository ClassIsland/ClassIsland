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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ClassIsland.Enums;
using ClassIsland.Models;
using ClassIsland.ViewModels;
using MaterialDesignThemes.Wpf;
using MdXaml;
using Microsoft.Toolkit.Uwp.Notifications;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ClassIsland.Views;
/// <summary>
/// SettingsWindow.xaml 的交互逻辑
/// </summary>
public partial class SettingsWindow : Window
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

    private TaskBarIconService TaskBarIconService
    {
        get;
    }

    public SettingsWindow()
    {
        UpdateService = App.GetService<UpdateService>();
        TaskBarIconService = App.GetService<TaskBarIconService>();
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
}
