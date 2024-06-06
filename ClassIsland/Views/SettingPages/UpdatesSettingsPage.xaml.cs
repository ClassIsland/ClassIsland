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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.Services.AppUpdating;
using ClassIsland.Shared.Enums;
using ClassIsland.ViewModels.SettingsPages;
using MaterialDesignThemes.Wpf;
using MdXaml;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// UpdatesSettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("update", "更新", PackIconKind.UploadOutline, PackIconKind.Upload, SettingsPageCategory.Internal)]
public partial class UpdatesSettingsPage : SettingsPageBase
{
    public SettingsService SettingsService { get; }

    public UpdateService UpdateService { get; }

    public UpdateSettingsViewModel ViewModel { get; } = new();

    public UpdatesSettingsPage(SettingsService settingsService, UpdateService updateService)
    {
        DataContext = this;
        SettingsService = settingsService;
        UpdateService = updateService;
        SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
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
            case nameof(SettingsService.Settings.SpeechSource):
                RequestRestart();
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
        var fd = e.Transform(SettingsService.Settings.UpdateReleaseInfo);
        fd.FontFamily = (FontFamily)FindResource("HarmonyOsSans");
        ViewModel.CurrentMarkdownDocument = fd;
    }

    private void RefreshDescription()
    {
        ViewModel.SelectedChannelModel = UpdateService.UpdateChannels.FirstOrDefault(c => c.RootUrl == SettingsService.Settings.SelectedChannel) ?? new UpdateChannel();
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
        if (!File.Exists(".\\UpdateTemp\\update.zip"))
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

    private void ButtonChangelogs_OnClick(object sender, RoutedEventArgs e)
    {
        App.GetService<MainWindow>().OpenHelpsWindow();
        App.GetService<HelpsWindow>().InitDocumentName = "新增功能";
        App.GetService<HelpsWindow>().ViewModel.SelectedDocumentName = "新增功能";
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
}