using ClassIsland.Core.Abstractions.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Services;
using ClassIsland.Shared.Abstraction.Services;
using ClassIsland.ViewModels.SettingsPages;
using MaterialDesignThemes.Wpf;
using System.Diagnostics;
using System.Windows.Input;
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
[SettingsPageInfo("notification", "提醒", PackIconKind.BellNotificationOutline, PackIconKind.BellNotification, SettingsPageCategory.Internal)]
public partial class NotificationSettingsPage : SettingsPageBase
{
    public SettingsService SettingsService { get; }

    public INotificationHostService NotificationHostService { get; }

    public ISpeechService SpeechService { get; }

    public IManagementService ManagementService { get; }

    public NotificationSettingsViewModel ViewModel { get; } = new();
    public NotificationSettingsPage(SettingsService settingsService, INotificationHostService notificationHostService, ISpeechService speechService, IManagementService managementService)
    {
        InitializeComponent();
        DataContext = this;
        SettingsService = settingsService;
        NotificationHostService = notificationHostService;
        SpeechService = speechService;
        ManagementService = managementService;
    }

    private void UpdateSpeechProviderSettingsControl()
    {
        var info = SpeechProviderRegistryService.RegisteredProviders.FirstOrDefault(x =>
            x.Id == SettingsService.Settings.SelectedSpeechProvider);
        if (info == null)
        {
            return;
        }

        ViewModel.SpeechProviderSettingsControl = IAppHost.Host?.Services.GetKeyedService<SpeechProviderControlBase>(info.Id);
    }

    private void CollectionViewSourceNotificationProviders_OnFilter(object sender, FilterEventArgs e)
    {
        var i = e.Item as string;
        if (i == null)
            return;
        e.Accepted = NotificationHostService.NotificationProviders.FirstOrDefault(x => x.ProviderGuid.ToString() == i) != null;
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

        ViewModel.SelectedRegisterInfo = NotificationHostService.NotificationProviders.FirstOrDefault(x => x.ProviderGuid.ToString() == ViewModel.NotificationSettingsSelectedProvider);
    }

    private void ButtonTestSpeeching_OnClick(object sender, RoutedEventArgs e)
    {
        SpeechService.ClearSpeechQueue();
        SpeechService.EnqueueSpeechQueue(ViewModel.TestSpeechText);
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
        SettingsService.Settings.PropertyChanged -= Settings_PropertyChanged;
    }

    private void SelectorChannel_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        e.Handled = true;
        ViewModel.IsNotificationSettingsPanelOpened = true;
        if (ViewModel.NotificationSettingsSelectedChannel == null)
        {
            return;
        }
        ViewModel.SelectedRegisterInfo = NotificationHostService.NotificationProviders.FirstOrDefault(x => x.ProviderGuid.ToString() == ViewModel.NotificationSettingsSelectedProvider)?.NotificationChannels.FirstOrDefault(x => x.ProviderGuid.ToString() == ViewModel.NotificationSettingsSelectedChannel);
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

    private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        //e.Handled = true;
    }

    private void Expander_OnCollapsed(object sender, RoutedEventArgs e)
    {
        ViewModel.NotificationSettingsSelectedChannel = null;
        SetCurrentNotificationProvider();
    }

    private void NotificationSettingsPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.PropertyChanged += Settings_PropertyChanged;
        UpdateSpeechProviderSettingsControl();
    }

    private void ButtonOpenAdvancedProviderSettings_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("NotificationSettingsDrawer", dataContext: ViewModel.SelectedRegisterInfo);
    }
}