using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using EdgeTTS;

namespace ClassIsland.ViewModels.SettingsPages;

public class NotificationSettingsViewModel : ObservableRecipient
{
    private bool _isNotificationSettingsPanelOpened = false;
    private string? _notificationSettingsSelectedProvider;

    public bool IsNotificationSettingsPanelOpened
    {
        get => _isNotificationSettingsPanelOpened;
        set
        {
            if (value == _isNotificationSettingsPanelOpened) return;
            _isNotificationSettingsPanelOpened = value;
            OnPropertyChanged();
        }
    }

    public string? NotificationSettingsSelectedProvider
    {
        get => _notificationSettingsSelectedProvider;
        set
        {
            if (value == _notificationSettingsSelectedProvider) return;
            _notificationSettingsSelectedProvider = value;
            OnPropertyChanged();
        }
    }

    public List<Voice> EdgeVoices { get; } = JsonSerializer.Deserialize<List<Voice>>(Application.GetResourceStream(new Uri("/Assets/EdgeTts/VoiceList.json", UriKind.RelativeOrAbsolute))?.Stream ?? Stream.Null)?.FindAll(i => i.Locale.Contains("zh-CN")) ?? new();

    public string TestSpeechText { get; set; } = "风带来了故事的种子，时间使之发芽。";
}