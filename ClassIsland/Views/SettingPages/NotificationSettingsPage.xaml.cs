﻿using ClassIsland.Core.Abstractions.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Services;
using ClassIsland.Shared.Abstraction.Services;
using ClassIsland.ViewModels.SettingsPages;
using MaterialDesignThemes.Wpf;
using System.Diagnostics;
using ClassIsland.Core.Enums.SettingsWindow;

namespace ClassIsland.Views.SettingPages;

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
        SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        //Console.WriteLine(e.PropertyName);
        switch (e.PropertyName)
        {
            case nameof(SettingsService.Settings.SpeechSource):
                RequestRestart();
                break;
        }
    }

    private void CollectionViewSourceNotificationProviders_OnFilter(object sender, FilterEventArgs e)
    {
        var i = e.Item as string;
        if (i == null)
            return;
        e.Accepted = NotificationHostService.NotificationProviders.FirstOrDefault(x => x.ProviderGuid.ToString() == i) != null;
    }

    private void Selector_OnSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
        {
            ViewModel.IsNotificationSettingsPanelOpened = false;
            return;
        }
        ViewModel.IsNotificationSettingsPanelOpened = true;
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

     // 新增的 GPTSoVITS 测试语音按钮事件处理
    private void ButtonTestSpeechingGPTSoVITS_OnClick(object sender, RoutedEventArgs e)
    {
        // 调用 GPTSoVITS 服务进行测试语音
        // 确保 ViewModel 和 SpeechService 已正确配置
        SpeechService.ClearSpeechQueue();
        SpeechService.EnqueueSpeechQueue(ViewModel.TestSpeechTextGPTSoVITS);
    }

    // 新增的 GPTSoVITS 打开设置按钮事件处理
    private void ButtonOpenSpeechSettingsGPTSoVITS_OnClick(object sender, RoutedEventArgs e)
    {
        // 打开 GPTSoVITS 的相关设置页面
        // 请将 URL 替换为实际的 GPTSoVITS 设置页面地址
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://gptsovits.example.com/settings", // 替换为实际 URL
            UseShellExecute = true
        });
    }
}