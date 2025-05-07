﻿using ClassIsland.Core.Abstractions.Controls;
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
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Models;
using ClassIsland.Shared.Helpers;
using CommunityToolkit.Mvvm.Input;

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

    public GptSoVitsSpeechSettingsList GptSoVitsSpeechSettingsPresets { get; }

    public static readonly string GptSoVitsSettingsPresetsPath = System.IO.Path.Combine(App.AppConfigPath,
        "GptSovitsSettingsPresets.json");

    public static readonly GptSoVitsSpeechSettingsList InternalGptSoVitsSpeechPresets = [
        new GptSoVitsSpeechSettings
        {
            PresetName = "派蒙（原神）",
            GptSoVitsVoiceName = "paimon",
            IsInternal = true,
            GptSoVitsServerIp = "paimon.tts.wez.ink",
            GptSoVitsBatchSize = 5,
            GptSoVitsPort = "80",
            GptSoVitsPromptText = "既然罗莎莉亚说足迹上有元素力，用元素视野应该能很清楚地看到吧。",
            GptSoVitsRefAudioPath = "template_audio/paimon.wav"
        },
        new GptSoVitsSpeechSettings
        {
            PresetName = "可莉（原神）",
            GptSoVitsVoiceName = "klee",
            IsInternal = true,
            GptSoVitsServerIp = "klee.tts.wez.ink",
            GptSoVitsBatchSize = 5,
            GptSoVitsPort = "80",
            GptSoVitsPromptText = "买东西那天还有一个人一起帮着看了款式，那个人好像叫",
            GptSoVitsRefAudioPath = "template_audio/klee.wav"
        },
        new GptSoVitsSpeechSettings
        {
            PresetName = "神里绫华（原神）",
            GptSoVitsVoiceName = "ayaka",
            IsInternal = true,
            GptSoVitsServerIp = "ayaka.tts.wez.ink",
            GptSoVitsBatchSize = 5,
            GptSoVitsPort = "80",
            GptSoVitsPromptText = "这里有别于神里家的布景，移步之间，处处都有新奇感。",
            GptSoVitsRefAudioPath = "template_audio/ayaka.wav"
        },
        new GptSoVitsSpeechSettings
        {
            PresetName = "爱莉希雅（崩坏3）",
            GptSoVitsVoiceName = "elysia",
            IsInternal = true,
            GptSoVitsServerIp = "elysia.tts.wez.ink",
            GptSoVitsBatchSize = 5,
            GptSoVitsPort = "80",
            GptSoVitsPromptText = "他这么向我说道，悲剧并非终结，而是希望的起始。",
            GptSoVitsRefAudioPath = "template_audio/elysia.wav"
        },
        new GptSoVitsSpeechSettings
        {
            PresetName = "钟离（原神）",
            GptSoVitsVoiceName = "zhongli",
            IsInternal = true,
            GptSoVitsServerIp = "zhongli.tts.wez.ink",
            GptSoVitsBatchSize = 5,
            GptSoVitsPort = "80",
            GptSoVitsPromptText = "无事逢客休，席上校两棋…我们开局吧。",
            GptSoVitsRefAudioPath = "template_audio/zhongli.wav"
        },
        new GptSoVitsSpeechSettings
        {
            PresetName = "流萤（崩坏：星穹铁道）",
            GptSoVitsVoiceName = "firefly",
            IsInternal = true,
            GptSoVitsServerIp = "firefly.tts.wez.ink",
            GptSoVitsBatchSize = 5,
            GptSoVitsPort = "80",
            GptSoVitsPromptText = "因为你身上别着星穹列车的徽章呀，我在大银幕上见过！",
            GptSoVitsRefAudioPath = "template_audio/firefly.wav"
        },
        new GptSoVitsSpeechSettings
        {
            PresetName = "高考听力男声",
            GptSoVitsVoiceName = "highexam-male",
            IsInternal = true,
            GptSoVitsServerIp = "highexam-male.tts.wez.ink",
            GptSoVitsBatchSize = 5,
            GptSoVitsPort = "80",
            GptSoVitsPromptText = "回答听力部分时，请先将答案标在试卷上。",
            GptSoVitsRefAudioPath = "template_audio/highexam-male.wav"
        }
        ,
        new GptSoVitsSpeechSettings
        {
            PresetName = "三月七（崩坏：星穹铁道）",
            GptSoVitsVoiceName = "march7th",
            IsInternal = true,
            GptSoVitsServerIp = "march7th.tts.wez.ink",
            GptSoVitsBatchSize = 5,
            GptSoVitsPort = "80",
            GptSoVitsPromptText = "名字是我自己取的，大家也叫我三月、小三月…你呢？你想叫我什么？",
            GptSoVitsRefAudioPath = "template_audio/march7th.wav"
        }
        ];        

    public NotificationSettingsPage(SettingsService settingsService, INotificationHostService notificationHostService, ISpeechService speechService, IManagementService managementService)
    {
        InitializeComponent();
        DataContext = this;
        SettingsService = settingsService;
        NotificationHostService = notificationHostService;
        SpeechService = speechService;
        ManagementService = managementService;
        SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
        
        GptSoVitsSpeechSettingsPresets =
            ConfigureFileHelper.LoadConfig<GptSoVitsSpeechSettingsList>(GptSoVitsSettingsPresetsPath);
        foreach (var preset in GptSoVitsSpeechSettingsPresets.Where(x => x.IsInternal).ToList())
        {
            GptSoVitsSpeechSettingsPresets.Remove(preset);
        }

        foreach (var preset in InternalGptSoVitsSpeechPresets)
        {
            GptSoVitsSpeechSettingsPresets.Insert(0, preset);
        }

        ConfigureFileHelper.SaveConfig(GptSoVitsSettingsPresetsPath, GptSoVitsSpeechSettingsPresets);
        GptSoVitsSpeechSettingsPresets.CollectionChanged += (_, _) => ConfigureFileHelper.SaveConfig(GptSoVitsSettingsPresetsPath, GptSoVitsSpeechSettingsPresets);
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
        SpeechService.EnqueueSpeechQueue(ViewModel.TestSpeechText,true);
    }

    private void ButtonOpenSpeechSettings_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(@"C:\WINDOWS\system32\rundll32.exe", @"shell32.dll,Control_RunDLL C:\WINDOWS\system32\Speech\SpeechUX\sapi.cpl");
    }


    private void ButtonSaveGptSovitsPreset_OnClick(object sender, RoutedEventArgs e)
    {
        SaveGptSovitsPreset();
    }

    private void ButtonOpenGptSovitsPresetsDrawer_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("SpeechPresets");
    }

    [RelayCommand]
    private void LoadGptSoVitsPreset(GptSoVitsSpeechSettings settings)
    {
        SettingsService.Settings.GptSoVitsSpeechSettings = ConfigureFileHelper.CopyObject(settings);
    }

    [RelayCommand]
    private void OverwriteGptSoVitsPreset(GptSoVitsSpeechSettings settings)
    {
        GptSoVitsSpeechSettingsPresets.Insert(GptSoVitsSpeechSettingsPresets.IndexOf(settings), ConfigureFileHelper.CopyObject(SettingsService.Settings.GptSoVitsSpeechSettings));
        GptSoVitsSpeechSettingsPresets.Remove(settings);
    }

    [RelayCommand]
    private void DuplicateGptSoVitsPreset(GptSoVitsSpeechSettings settings)
    {
        var newPreset = ConfigureFileHelper.CopyObject(settings);
        newPreset.IsInternal = false;
        GptSoVitsSpeechSettingsPresets.Add(newPreset);
        ViewModel.SelectedGptSoVitsSpeechPreset = newPreset;
    }

    [RelayCommand]
    private void RemoveGptSoVitsPreset(GptSoVitsSpeechSettings settings)
    {
        GptSoVitsSpeechSettingsPresets.Remove(settings);
    }

    private void DataGridPresets_OnBeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
    {
        if (e.Row.Item is GptSoVitsSpeechSettings settings)
        {
            e.Cancel = settings.IsInternal;
        }
    }

    private void ButtonSaveGptSovitsPreset2_OnClick(object sender, RoutedEventArgs e)
    {
        SaveGptSovitsPreset();
        OpenDrawer("SpeechPresets");
    }

    private void SaveGptSovitsPreset()
    {
        var newPreset = ConfigureFileHelper.CopyObject(SettingsService.Settings.GptSoVitsSpeechSettings);
        newPreset.IsInternal = false;
        GptSoVitsSpeechSettingsPresets.Add(newPreset);
        ViewModel.SelectedGptSoVitsSpeechPreset = newPreset;
    }

    private void NotificationSettingsPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.PropertyChanged += Settings_PropertyChanged;
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SettingsService.Settings.NotificationUseStandaloneEffectUiThread))
        {
            RequestRestart();
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
}