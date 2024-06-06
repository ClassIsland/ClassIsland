using ClassIsland.Core.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Services;
using ClassIsland.Shared;
using ClassIsland.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AppCenter.Analytics;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Views;

/// <summary>
/// SettingsWindowNew.xaml 的交互逻辑
/// </summary>
public partial class SettingsWindowNew : MyWindow
{
    public SettingsNewViewModel ViewModel { get; } = new();

    [NotNull]
    public NavigationService? NavigationService { get; set; }

    private bool IsOpened { get; set; } = false;

    public IManagementService ManagementService { get; }

    private IHangService HangService { get; }

    public static readonly string StartupSettingsPage = "general";

    public SettingsWindowNew(IManagementService managementService, IHangService hangService)
    {
        InitializeComponent();
        DataContext = this;
        ManagementService = managementService;
        HangService = hangService;
        NavigationService = NavigationFrame.NavigationService;
        NavigationService.LoadCompleted += NavigationServiceOnLoadCompleted;
        NavigationService.Navigating += NavigationServiceOnNavigating;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        ViewModel.SelectedPageInfo =
            SettingsWindowRegistryService.Registered.FirstOrDefault(x => x.Id == StartupSettingsPage);
        base.OnContentRendered(e);
    }

    private async Task BeginStoryboardAsync(string key)
    {
        BeginStoryboard(key, out var complete);
        if (!complete.IsCancellationRequested)
            await Task.Run(() => complete.WaitHandle.WaitOne(), complete);
        await Dispatcher.Yield();
    }

    private void BeginStoryboard(string key, out CancellationToken cancellationToken)
    {
        var complete = new CancellationTokenSource();
        cancellationToken = complete.Token;
        if (!IsInitialized)
            return;
        if (FindResource(key) is not Storyboard sb)
            return;
        sb.Completed += (sender, args) =>
        {
            complete.Cancel();
        };
        sb.Begin();
    }

    private async void NavigationServiceOnNavigating(object sender, NavigatingCancelEventArgs e)
    {
       
    }

    private async void NavigationServiceOnLoadCompleted(object sender, NavigationEventArgs e)
    {
        if (e.ExtraData as SettingsWindowNavigationExtraData? == SettingsWindowNavigationExtraData.NavigateFromNavigationView)  
        {
            // 如果是从设置导航栏导航的，那么就要清除掉返回项目
            NavigationService.RemoveBackEntry();
            await Dispatcher.Yield();
            await BeginStoryboardAsync("NavigationEntering");
        }
        ViewModel.CanGoBack = NavigationService.CanGoBack;
    }


    private async void NavigationListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.IsViewCompressed)
        {
            ViewModel.IsNavigationDrawerOpened = false;
        }
        await BeginStoryboardAsync("NavigationLeaving");
        HangService.AssumeHang();
        // 从ioc容器获取页面
        var page = IAppHost.Host?.Services.GetKeyedService<SettingsPageBase>(ViewModel.SelectedPageInfo?.Id);
        // 清空抽屉
        ViewModel.IsDrawerOpen = false;
        ViewModel.DrawerContent = null;
        // 进行导航
        NavigationService.RemoveBackEntry();
        NavigationService.Navigate(page, SettingsWindowNavigationExtraData.NavigateFromNavigationView);
        //ViewModel.FrameContent;
        NavigationService.RemoveBackEntry();

    }

    private void SettingsWindowNew_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ViewModel.IsViewCompressed = Width < 800;
        if (!ViewModel.IsViewCompressed)
        {
            ViewModel.IsNavigationDrawerOpened = true;
        }
    }

    private void ButtonBaseToggleNavigationDrawer_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsNavigationDrawerOpened = !ViewModel.IsNavigationDrawerOpened;
    }

    private void ButtonGoBack_OnClick(object sender, RoutedEventArgs e)
    {
        NavigationService.GoBack();
    }

    public void Open()
    {
        if (!IsOpened)
        {
            Analytics.TrackEvent("打开设置窗口");
            IsOpened = true;
            Show();
        }
        else
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
        }
    }

    private void SettingsWindowNew_OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        IsOpened = false;
        Hide();
        GC.Collect();
    }

    private void CommandBindingOpenDrawer_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        ViewModel.DrawerContent = e.Parameter;
        ViewModel.IsDrawerOpen = true;
    }

    private void CommandBindingCloseDrawer_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        ViewModel.IsDrawerOpen = false;
    }

    private void ButtonRestartApp_OnClick(object sender, RoutedEventArgs e)
    {
        ShowRestartDialog();
    }

    private async void ShowRestartDialog()
    {
        if (DialogHost.IsDialogOpen(SettingsPageBase.DialogHostIdentifier))
            return;
        var r = await DialogHost.Show(FindResource("RestartDialog"), SettingsPageBase.DialogHostIdentifier);
        if (r as bool? != true)
            return;
        App.Restart();
    }

    private void CommandBindingRestartApp_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        ViewModel.IsRequestedRestart = true;
        ShowRestartDialog();
    }
}