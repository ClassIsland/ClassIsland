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
using Application = System.Windows.Application;

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

    public SettingsWindow()
    {
        UpdateService = App.GetService<UpdateService>();
        InitializeComponent();
        DataContext = this;
    }

    protected override void OnInitialized(EventArgs e)
    {
        RefreshMonitors();
        var r = new StreamReader(Application.GetResourceStream(new Uri("/Assets/LICENSE.txt", UriKind.Relative))!.Stream);
        ViewModel.License = r.ReadToEnd();
        base.OnInitialized(e);
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
        Settings.SelectedChannel = "https://install.appcenter.ms/api/v0.1/apps/hellowrc/classisland/distribution_groups/publicbeta";
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
}
