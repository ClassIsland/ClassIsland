using CommunityToolkit.Mvvm.ComponentModel;
using Edge_tts_sharp.Model;
using Edge_tts_sharp;
using System.Collections.Generic;
using ClassIsland.Models;

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
        EdgeTts.GetVoice().FindAll(i => i.Locale.Contains("zh-CN"));

    // 现有的测试语音文本属性
    private string _testSpeechText = "风带来了故事的种子，时间使之发芽。";
    private GptSoVitsSpeechSettings? _selectedGptSoVitsSpeechPreset;

    public string TestSpeechText
    {
        get => _testSpeechText;
        set
        {
            if (_testSpeechText != value)
            {
                _testSpeechText = value;
                OnPropertyChanged();
            }
        }
    }

    public GptSoVitsSpeechSettings? SelectedGptSoVitsSpeechPreset
    {
        get => _selectedGptSoVitsSpeechPreset;
        set
        {
            if (Equals(value, _selectedGptSoVitsSpeechPreset)) return;
            _selectedGptSoVitsSpeechPreset = value;
            OnPropertyChanged();
        }
    }
}
