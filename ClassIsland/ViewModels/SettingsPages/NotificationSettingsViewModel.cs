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
        EdgeTts.GetVoice().FindAll(i => i.Locale.Contains("zh-CN"));

    // 现有的测试语音文本属性
    private string _testSpeechText = "风带来了故事的种子，时间使之发芽。";
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

    // GPTSoVITS 测试朗读文本
    private string _testSpeechTextGPTSoVITS = "风带来了故事的种子，时间使之发芽。";
    public string TestSpeechTextGPTSoVITS
    {
        get => _testSpeechTextGPTSoVITS;
        set
        {
            if (_testSpeechTextGPTSoVITS != value)
            {
                _testSpeechTextGPTSoVITS = value;
                OnPropertyChanged();
            }
        }
    }

    // GPTSoVITS 相关设置属性
    private string? _gptSoVITSServerIP;
    public string? GPTSoVITSServerIP
    {
        get => _gptSoVITSServerIP;
        set
        {
            if (_gptSoVITSServerIP != value)
            {
                _gptSoVITSServerIP = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _gptSoVITSPort;
    public string? GPTSoVITSPort
    {
        get => _gptSoVITSPort;
        set
        {
            if (_gptSoVITSPort != value)
            {
                _gptSoVITSPort = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _gptSoVITSVoiceName;
    public string? GPTSoVITSVoiceName
    {
        get => _gptSoVITSVoiceName;
        set
        {
            if (_gptSoVITSVoiceName != value)
            {
                _gptSoVITSVoiceName = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _gptSoVITSTextLang;
    public string? GPTSoVITSTextLang
    {
        get => _gptSoVITSTextLang;
        set
        {
            if (_gptSoVITSTextLang != value)
            {
                _gptSoVITSTextLang = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _gptSoVITSRefAudioPath;
    public string? GPTSoVITSRefAudioPath
    {
        get => _gptSoVITSRefAudioPath;
        set
        {
            if (_gptSoVITSRefAudioPath != value)
            {
                _gptSoVITSRefAudioPath = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _gptSoVITSPromptLang;
    public string? GPTSoVITSPromptLang
    {
        get => _gptSoVITSPromptLang;
        set
        {
            if (_gptSoVITSPromptLang != value)
            {
                _gptSoVITSPromptLang = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _gptSoVITSPromptText;
    public string? GPTSoVITSPromptText
    {
        get => _gptSoVITSPromptText;
        set
        {
            if (_gptSoVITSPromptText != value)
            {
                _gptSoVITSPromptText = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _gptSoVITSTextSplitMethod;
    public string? GPTSoVITSTextSplitMethod
    {
        get => _gptSoVITSTextSplitMethod;
        set
        {
            if (_gptSoVITSTextSplitMethod != value)
            {
                _gptSoVITSTextSplitMethod = value;
                OnPropertyChanged();
            }
        }
    }

    private int _gptSoVITSBatchSize;
    public int GPTSoVITSBatchSize
    {
        get => _gptSoVITSBatchSize;
        set
        {
            if (_gptSoVITSBatchSize != value)
            {
                _gptSoVITSBatchSize = value;
                OnPropertyChanged();
            }
        }
    }
}
