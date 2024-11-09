using CommunityToolkit.Mvvm.ComponentModel;
using Edge_tts_sharp.Model;
using Edge_tts_sharp;
using System.Collections.Generic;

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

    public List<eVoice> EdgeVoices { get; } =
        Edge_tts.GetVoice().FindAll(i => i.Locale.Contains("zh-CN"));

    public string TestSpeechText { get; set; } = "风带来了故事的种子，时间使之发芽。";
}