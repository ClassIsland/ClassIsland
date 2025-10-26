using System;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Enums.AppUpdating;
using ClassIsland.Services.AppUpdating;
using ClassIsland.Shared;
using ClassIsland.Shared.Enums;
using ClassIsland.ViewModels.SettingsPages;
using FluentAvalonia.UI.Controls;
using ReactiveUI;

namespace ClassIsland.Views.SettingPages;

[SettingsPageInfo("update", "更新", "\ue161", "\ue160", SettingsPageCategory.Internal)]
public partial class UpdateSettingsPage : SettingsPageBase
{
    private IDisposable? _updateSettingsObserver;
    
    public UpdateSettingsPageViewModel ViewModel { get; } = IAppHost.GetService<UpdateSettingsPageViewModel>();

    public static readonly FuncValueConverter<UpdateStatus, string> UpdateStatusToIconGlyphConverter =
        new(x => x switch
        {
            UpdateStatus.UpToDate => "\ue1a1",
            UpdateStatus.UpdateAvailable => "\ue161",
            UpdateStatus.UpdateDownloaded => "\ue0d3",
            UpdateStatus.UpdateDeployed => "\ue163",
            _ => ""
        });
    
    public static readonly FuncValueConverter<UpdateStatus, string> UpdateStatusToMessageConverter =
        new(x => x switch
        {
            UpdateStatus.UpToDate => "您已更新到最新版本。",
            UpdateStatus.UpdateAvailable => "检测到更新。" ,
            UpdateStatus.UpdateDownloaded => "已准备好安装更新。",
            UpdateStatus.UpdateDeployed => "更新已就绪。",
            _ => ""
        });
    
    public static readonly FuncValueConverter<UpdateWorkingStatus, string> UpdateWorkingStatusToMessageConverter =
        new(x => x switch
        {
            UpdateWorkingStatus.Idle => "就绪",
            UpdateWorkingStatus.CheckingUpdates => "正在检查更新…",
            UpdateWorkingStatus.DownloadingUpdates => "正在下载更新…",
            UpdateWorkingStatus.ExtractingUpdates => "正在部署更新…",
            _ => "???"
        });

    public static readonly FuncValueConverter<DownloadState, string> DownloadStateToMessageConverter =
        new(x => x switch
        {
            DownloadState.Pending => "等待下载",
            DownloadState.Downloading => "正在下载",
            DownloadState.Completed => "完成",
            DownloadState.Error => "错误",
            _ => "???"
        });
    
    public UpdateSettingsPage()
    {
        DataContext = this;
        InitializeComponent();
    }
    
    private async void ButtonCheckUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.UpdateService.CheckUpdateAsync();
    }

    private async void ButtonDownloadUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.UpdateService.DownloadUpdateAsync();
        await ViewModel.UpdateService.ExtractUpdateAsync();
    }

    private void UpdateChannelInfo()
    {
        ViewModel.SelectedChannel =
            ViewModel.UpdateService.DistributionMetadata.Channels.TryGetValue(
                ViewModel.SettingsService.Settings.SelectedUpdateChannelV3, out var v1)
                ? v1
                : ViewModel.SelectedChannel;
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        UpdateChannelInfo();
        _updateSettingsObserver ??= ViewModel.SettingsService.Settings
            .ObservableForProperty(x => x.SelectedUpdateChannelV3)
            .Subscribe(_ => UpdateChannelInfo());
    }


    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _updateSettingsObserver?.Dispose();
        _updateSettingsObserver = null;
    }

    private void ButtonOpenDownloadTasks_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenDrawer("DownloadInfoDrawer");
    }

    private async void ButtonCancelDownload_OnClick(object? sender, RoutedEventArgs e)
    {
        await ViewModel.UpdateService.StopDownloading();
    }

    private async void ButtonDeployUpdate_OnClick(object? sender, RoutedEventArgs e)
    {
        await ViewModel.UpdateService.ExtractUpdateAsync();
    }

    private void InfoBarError_OnCloseButtonClick(InfoBar sender, EventArgs args)
    {
        ViewModel.UpdateService.NetworkErrorException = null;
    }

    private void ButtonRestart_OnClick(object? sender, RoutedEventArgs e)
    {
        AppBase.Current.Restart(["-m"], true);
    }

    private async void SettingsExpanderItemCheckUpdateForce_OnClick(object? sender, RoutedEventArgs e)
    {
        await ViewModel.UpdateService.CheckUpdateAsync(true);
    }
}