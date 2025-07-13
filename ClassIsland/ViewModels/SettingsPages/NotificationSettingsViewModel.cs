using CommunityToolkit.Mvvm.ComponentModel;
using Edge_tts_sharp.Model;
using Edge_tts_sharp;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.Shared.Abstraction.Models.Notification;
using ClassIsland.Shared.Interfaces;
using DynamicData;
using DynamicData.Binding;

namespace ClassIsland.ViewModels.SettingsPages;

public class NotificationSettingsViewModel : ObservableRecipient
{
    public SettingsService SettingsService { get; }
    public INotificationHostService NotificationHostService { get; }
    public ISpeechService SpeechService { get; }
    public IManagementService ManagementService { get; }

    private readonly ReadOnlyObservableCollection<string> _notificationProviders;
    
    public ReadOnlyObservableCollection<string> NotificationProviders => _notificationProviders;
    
    
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

    public string? NotificationSettingsSelectedChannel
    {
        get => _notificationSettingsSelectedChannel;
        set
        {
            if (value == _notificationSettingsSelectedChannel) return;
            _notificationSettingsSelectedChannel = value;
            OnPropertyChanged();
        }
    }

    public INotificationSenderRegisterInfo? SelectedRegisterInfo
    {
        get => _selectedRegisterInfo;
        set
        {
            if (Equals(value, _selectedRegisterInfo)) return;
            _selectedRegisterInfo = value;
            OnPropertyChanged();
        }
    }

    public List<eVoice> EdgeVoices { get; } =
        EdgeTts.GetVoice().FindAll(i => i.Locale.Contains("zh-CN"));

    // 现有的测试语音文本属性
    private string _testSpeechText = "风带来了故事的种子，时间使之发芽。";
    private GptSoVitsSpeechSettings? _selectedGptSoVitsSpeechPreset;
    private INotificationSenderRegisterInfo? _selectedRegisterInfo;
    private string? _notificationSettingsSelectedChannel;
    private object? _speechProviderSettingsControl;

    public NotificationSettingsViewModel(SettingsService settingsService,
        INotificationHostService notificationHostService,
        ISpeechService speechService,
        IManagementService managementService)
    {
        SettingsService = settingsService;
        NotificationHostService = notificationHostService;
        SpeechService = speechService;
        ManagementService = managementService;

        SettingsService.Settings.NotificationProvidersPriority
            .ToObservableChangeSet()
            .Filter(x => NotificationHostService.NotificationProviders.Any(y => y.ProviderGuid.ToString() == x))
            .Bind(out _notificationProviders);
    }

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

    public object? SpeechProviderSettingsControl
    {
        get => _speechProviderSettingsControl;
        set
        {
            if (Equals(value, _speechProviderSettingsControl)) return;
            _speechProviderSettingsControl = value;
            OnPropertyChanged();
        }
    }
}
