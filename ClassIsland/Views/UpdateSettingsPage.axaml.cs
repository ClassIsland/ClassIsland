using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;
using ClassIsland.Shared.Enums;
using ClassIsland.ViewModels.SettingsPages;
using DynamicData.Kernel;
using PhainonDistributionCenter.Shared.Models.Client;
using ReactiveUI;

namespace ClassIsland.Views;

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
    
    public UpdateSettingsPage()
    {
        DataContext = this;
        InitializeComponent();
    }
    
    private async void ButtonCheckUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.UpdateService.CheckUpdateAsync();
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
}