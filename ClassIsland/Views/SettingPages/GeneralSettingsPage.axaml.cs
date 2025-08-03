using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
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
    public GeneralSettingsViewModel ViewModel { get; } = IAppHost.GetService<GeneralSettingsViewModel>();

    public GeneralSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SettingsService.Settings.IsWaitForTransientDisabled) or nameof(SettingsService.Settings.AnimationLevel))
        {
            RequestRestart();
        }
    }

    private void ButtonSyncTimeNow_OnClick(object sender, RoutedEventArgs e)
    {
        _ = Task.Run(ViewModel.ExactTimeService.Sync);
    }

    private void ButtonCloseMigrationTip_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.ShowComponentsMigrateTip = false;
    }

    private void ButtonWeekOffsetSettingsButtons_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsWeekOffsetSettingsOpen = false;
    }

    private void ButtonWeekOffsetSettingsOpen_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsWeekOffsetSettingsOpen = true;
    }

    private void ButtonCloseSellingAnnouncementBanner_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.ShowSellingAnnouncement = false;
    }

    private void GeneralSettingsPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
    }

    private void GeneralSettingsPage_OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.PropertyChanged -= SettingsOnPropertyChanged;
    }

    private void ButtonAdjustTime_OnClick(object sender, RoutedEventArgs e)
    {
        var window = IAppHost.GetService<TimeAdjustmentWindow>();
        window.ShowDialog((TopLevel.GetTopLevel(this) as Window)!);
    }
}

