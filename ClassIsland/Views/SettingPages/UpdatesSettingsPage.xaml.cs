using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Helpers;
using ClassIsland.Services;
using ClassIsland.Services.AppUpdating;
using ClassIsland.Shared.Enums;
using ClassIsland.ViewModels.SettingsPages;
using MaterialDesignThemes.Wpf;
using Sentry;
using Path = System.IO.Path;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// UpdatesSettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("update", "更新", PackIconKind.UploadOutline, PackIconKind.Upload, SettingsPageCategory.Internal)]
public partial class UpdatesSettingsPage : SettingsPageBase
{
    public SettingsService SettingsService { get; }

    public UpdateService UpdateService { get; }
    public IManagementService ManagementService { get; }

    public UpdateSettingsViewModel ViewModel { get; } = new();

    public static readonly DependencyProperty IsEasterEggTriggeredProperty =
    DependencyProperty.Register(nameof(IsEasterEggTriggered), typeof(bool), typeof(UpdatesSettingsPage),
        new PropertyMetadata(false));

    public bool IsEasterEggTriggered
    {
        get => (bool)GetValue(IsEasterEggTriggeredProperty);
        set => SetValue(IsEasterEggTriggeredProperty, value);
    }

    public UpdatesSettingsPage(SettingsService settingsService, UpdateService updateService, IManagementService managementService)
    {
        DataContext = this;
        SettingsService = settingsService;
        UpdateService = updateService;
        ManagementService = managementService;
        InitializeComponent();
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        UpdateCache();
        RefreshDescription();
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        //Console.WriteLine(e.PropertyName);
        switch (e.PropertyName)
        {
            case nameof(SettingsService.Settings.UpdateReleaseInfo):
                UpdateCache();
                break;
        }

        RefreshDescription();
    }

    private void UpdateCache()
    {
        ViewModel.CurrentMarkdownDocument = MarkdownConvertHelper.ConvertMarkdown(UpdateService.SelectedVersionInfo.ChangeLogs);
    }

    private void RefreshDescription()
    {
        if (UpdateService.Index.Channels.TryGetValue(SettingsService.Settings.SelectedUpdateChannelV2, out var channelInfo))
        {
            ViewModel.SelectedChannelModel = channelInfo;
        }
    }

    private void UpdateErrorMessage_OnActionClick(object sender, RoutedEventArgs e)
    {
        UpdateService.NetworkErrorException = null;
    }

    private async void ButtonCheckUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        await UpdateService.CheckUpdateAsync();
    }

    private async void ButtonStartDownloading_OnClick(object sender, RoutedEventArgs e)
    {
        await UpdateService.DownloadUpdateAsync();
    }

    private async void ButtonRestartToUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(Path.Combine(UpdateService.UpdateTempPath, "update.zip")))
            return;
        await UpdateService.RestartAppToUpdateAsync();
    }

    private void ButtonCancelUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        if (UpdateService.CurrentWorkingStatus == UpdateWorkingStatus.DownloadingUpdates)
        {
            UpdateService.StopDownloading();
        }

        if (SettingsService.Settings.LastUpdateStatus == UpdateStatus.UpdateDownloaded)
        {
            _ = UpdateService.RemoveDownloadedFiles();
        }
    }

    private void ButtonDebugResetUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.LastUpdateStatus = UpdateStatus.UpToDate;
    }

    private void ButtonDebugDownloaded_OnClick(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.LastUpdateStatus = UpdateStatus.UpdateDownloaded;
    }

    private void ButtonDebugNetworkError_OnClick(object sender, RoutedEventArgs e)
    {
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

    private async void ButtonChangelogs_OnClick(object sender, RoutedEventArgs e)
    {
        var stream = Application
            .GetResourceStream(new Uri("/Assets/Documents/ChangeLog.md", UriKind.RelativeOrAbsolute))?.Stream;
        if (stream == null)
        {
            return;
        }

        SentrySdk.Metrics.Increment("views.update.changelog.open");
        ViewModel.ChangeLogs = MarkdownConvertHelper.ConvertMarkdown(await new StreamReader(stream).ReadToEndAsync());
        OpenDrawer("ChangeLogsDrawer");
    }

    private async void MenuItemTestUpdateMirrors_OnClick(object sender, RoutedEventArgs e)
    {
        await App.GetService<UpdateNodeSpeedTestingService>().RunSpeedTestAsync();
    }

    private void ButtonAdvancedSettings_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("AdvancedUpdateSettings");
    }

    private async void ButtonForceUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        CloseDrawer();
        await UpdateService.CheckUpdateAsync(isForce: true);
    }

    private void IconUpdateStatus_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ManagementService.Policy.DisableEasterEggs)
        {
            return;
        }
        IsEasterEggTriggered = true;
    }
    private void UpdateServiceOnUpdateInfoUpdated(object? sender, EventArgs e)
    {
        UpdateCache();
        RefreshDescription();
    }

    private void UpdatesSettingsPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
        UpdateService.UpdateInfoUpdated += UpdateServiceOnUpdateInfoUpdated;
    }


    private void UpdatesSettingsPage_OnUnloaded(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.PropertyChanged -= SettingsOnPropertyChanged;
        UpdateService.UpdateInfoUpdated -= UpdateServiceOnUpdateInfoUpdated;
    }
}