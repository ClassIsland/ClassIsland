using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Abstractions.Services.Metadata;
using ClassIsland.Core.Abstractions.Views;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.ViewModels.SettingsPages;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// GeneralSettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("general", "基本", SettingsPageCategory.Internal)]
public partial class GeneralSettingsPage : SettingsPageBase
{
    public SettingsService SettingsService { get; }

    public IManagementService ManagementService { get; }

    public IExactTimeService ExactTimeService { get; }

    public MiniInfoProviderHostService MiniInfoProviderHostService { get; }
    public ISplashService SplashService { get; }
    public IAnnouncementService AnnouncementService { get; }

    public GeneralSettingsViewModel ViewModel { get; } = new();

    public GeneralSettingsPage(SettingsService settingsService, IManagementService managementService, IExactTimeService exactTimeService, MiniInfoProviderHostService miniInfoProviderHostService, ISplashService splashService, IAnnouncementService announcementService)
    {
        InitializeComponent();
        DataContext = this;
        SettingsService = settingsService;
        ManagementService = managementService;
        ExactTimeService = exactTimeService;
        MiniInfoProviderHostService = miniInfoProviderHostService;
        SplashService = splashService;
        AnnouncementService = announcementService;
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SettingsService.Settings.IsTransientDisabled) or nameof(SettingsService.Settings.IsWaitForTransientDisabled))
        {
            RequestRestart();
        }
    }

    private void ButtonSyncTimeNow_OnClick(object sender, RoutedEventArgs e)
    {
        _ = Task.Run(ExactTimeService.Sync);
    }

    private void ButtonCloseMigrationTip_OnClick(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.ShowComponentsMigrateTip = false;
    }

    private void ButtonWeekOffsetSettingsButtons_OnClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Button)
        {
            return;
        }
        ViewModel.IsWeekOffsetSettingsOpen = false;
    }

    private void ButtonWeekOffsetSettingsOpen_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsWeekOffsetSettingsOpen = true;
    }

    private void ButtonCloseSellingAnnouncementBanner_OnClick(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.ShowSellingAnnouncement = false;
    }

    private async void ButtonRefreshSplashPreview_OnClick(object sender, RoutedEventArgs e)
    {
        SplashService.ResetSplashText();
        var splashWindow = IAppHost.GetService<SplashWindowBase>();
        splashWindow.Show();
        await Task.Delay(TimeSpan.FromSeconds(3));
        SplashService.EndSplash();
    }

    private void GeneralSettingsPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
    }

    private void GeneralSettingsPage_OnUnloaded(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.PropertyChanged -= SettingsOnPropertyChanged;
    }

    private void ButtonAdjustTime_OnClick(object sender, RoutedEventArgs e)
    {
        var window = IAppHost.GetService<TimeAdjustmentWindow>();
        window.Owner = Window.GetWindow(this);
        window.ShowDialog();
    }
}