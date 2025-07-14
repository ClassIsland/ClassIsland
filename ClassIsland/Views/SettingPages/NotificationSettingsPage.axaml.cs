using System.Collections.Generic;
using ClassIsland.Core.Abstractions.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Services;
using ClassIsland.Shared.Abstraction.Services;
using ClassIsland.ViewModels.SettingsPages;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Services.Registry;
using ClassIsland.Models;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Views.SettingPages;

using GptSoVitsSpeechSettingsList = ObservableCollection<GptSoVitsSpeechSettings>;

/// <summary>
/// NotificationSettingsPage.xaml 的交互逻辑
/// </summary>
[FullWidthPage]
[SettingsPageInfo("notification", "提醒", "\ue02b", "\ue02a", SettingsPageCategory.Internal)]
public partial class NotificationSettingsPage : SettingsPageBase
{
    public static readonly List<FilePickerFileType> AudioFileTypes = [
        new FilePickerFileType("音频文件")
        {
            MimeTypes = ["audio"]
        }
    ];

    public NotificationSettingsViewModel ViewModel { get; } = IAppHost.GetService<NotificationSettingsViewModel>();
    public NotificationSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void UpdateSpeechProviderSettingsControl()
    {
        var info = SpeechProviderRegistryService.RegisteredProviders.FirstOrDefault(x =>
            x.Id == ViewModel.SettingsService.Settings.SelectedSpeechProvider);
        if (info == null)
        {
            return;
        }

        ViewModel.SpeechProviderSettingsControl = IAppHost.Host?.Services.GetKeyedService<SpeechProviderControlBase>(info.Id);
    }

    private void SelectorMain_OnSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
        {
            ViewModel.IsNotificationSettingsPanelOpened = false;
            return;
        }

        if (e.RemovedItems.Count >= 1 && e.RemovedItems[0] == e.AddedItems[0])
        {
            return;
        }
        ViewModel.IsNotificationSettingsPanelOpened = true;
        SetCurrentNotificationProvider();
    }

    private void SetCurrentNotificationProvider()
    {
        if (ViewModel.NotificationSettingsSelectedProvider == null)
        {
            return;
        }

        ViewModel.SelectedRegisterInfo = ViewModel.NotificationHostService.NotificationProviders.FirstOrDefault(x => x.ProviderGuid.ToString() == ViewModel.NotificationSettingsSelectedProvider);
    }

    private void ButtonTestSpeeching_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SpeechService.ClearSpeechQueue();
        ViewModel.SpeechService.EnqueueSpeechQueue(ViewModel.TestSpeechText);
    }

    private void ButtonOpenSpeechSettings_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(@"C:\WINDOWS\system32\rundll32.exe", @"shell32.dll,Control_RunDLL C:\WINDOWS\system32\Speech\SpeechUX\sapi.cpl");
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SettingsService.Settings.SelectedSpeechProvider):
                UpdateSpeechProviderSettingsControl();
                RequestRestart();
                break;
            case nameof(SettingsService.Settings.NotificationUseStandaloneEffectUiThread):
                RequestRestart();
                break;
        }
    }

    private void NotificationSettingsPage_OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.PropertyChanged -= Settings_PropertyChanged;
    }

    private void SelectorChannel_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        e.Handled = true;
        ViewModel.IsNotificationSettingsPanelOpened = true;
        if (ViewModel.NotificationSettingsSelectedChannel == null)
        {
            return;
        }
        ViewModel.SelectedRegisterInfo = ViewModel.NotificationHostService.NotificationProviders.FirstOrDefault(x => x.ProviderGuid.ToString() == ViewModel.NotificationSettingsSelectedProvider)?.NotificationChannels.FirstOrDefault(x => x.ProviderGuid.ToString() == ViewModel.NotificationSettingsSelectedChannel);
    }

    private void Expander_OnCollapsed(object sender, RoutedEventArgs e)
    {
        ViewModel.NotificationSettingsSelectedChannel = null;
        SetCurrentNotificationProvider();
    }

    private void NotificationSettingsPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.PropertyChanged += Settings_PropertyChanged;
        UpdateSpeechProviderSettingsControl();
    }

    private void ButtonOpenAdvancedProviderSettings_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("NotificationSettingsDrawer", dataContext: ViewModel.SelectedRegisterInfo);
    }
}