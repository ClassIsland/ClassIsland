using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using ClassIsland.Core.Interfaces;
using ClassIsland.Models;
using ClassIsland.Models.Weather;

using CommunityToolkit.Mvvm.ComponentModel;

using Edge_tts_sharp;
using Edge_tts_sharp.Model;

using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;

namespace ClassIsland.ViewModels;

public class SettingsViewModel : ObservableRecipient
{
    private Screen[] _screens = Array.Empty<Screen>();
    private string _license = "";
    private object? _drawerContent = new();
    private FlowDocument _currentMarkdownDocument = new();
    private UpdateChannel _selectedChannelModel = new();
    private int _appIconClickCount = 0;
    private BitmapImage _testImage = new();
    private string _debugOutputs = "";
    private ObservableCollection<Color> _debugImageAccentColors = new();
    private bool _isNotificationSettingsPanelOpened = false;
    private string? _notificationSettingsSelectedProvider = null;
    private bool _isPopupMenuOpened = false;
    private KeyValuePair<string, IMiniInfoProvider>? _selectedMiniInfoProvider;
    private List<City> _citySearchResults = new();
    private object? _weatherNotificationControlTest;
    private string _diagnosticInfo = "";
    private bool _isMoreNotificationSettingsExpanded = false;
    private string _testSpeechText = "风带来了故事的种子，时间使之发芽。";
    private bool _isRestartRequired = false;
    private bool _isRefreshingContributors = false;

    public Screen[] Screens
    {
        get => _screens;
        set
        {
            if (Equals(value, _screens)) return;
            _screens = value;
            OnPropertyChanged();
        }
    }

    public string License
    {
        get => _license;
        set
        {
            if (value == _license) return;
            _license = value;
            OnPropertyChanged();
        }
    }

    public object? DrawerContent
    {
        get => _drawerContent;
        set
        {
            if (Equals(value, _drawerContent)) return;
            _drawerContent = value;
            OnPropertyChanged();
        }
    }

    public FlowDocument CurrentMarkdownDocument
    {
        get => _currentMarkdownDocument;
        set
        {
            if (Equals(value, _currentMarkdownDocument)) return;
            _currentMarkdownDocument = value;
            OnPropertyChanged();
        }
    }

    public UpdateChannel SelectedChannelModel
    {
        get => _selectedChannelModel;
        set
        {
            if (Equals(value, _selectedChannelModel)) return;
            _selectedChannelModel = value;
            OnPropertyChanged();
        }
    }

    public int AppIconClickCount
    {
        get => _appIconClickCount;
        set
        {
            if (value == _appIconClickCount) return;
            _appIconClickCount = value;
            OnPropertyChanged();
        }
    }

    public BitmapImage TestImage
    {
        get => _testImage;
        set
        {
            if (Equals(value, _testImage)) return;
            _testImage = value;
            OnPropertyChanged();
        }
    }

    public string DebugOutputs
    {
        get => _debugOutputs;
        set
        {
            if (value == _debugOutputs) return;
            _debugOutputs = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Color> DebugImageAccentColors
    {
        get => _debugImageAccentColors;
        set
        {
            if (Equals(value, _debugImageAccentColors)) return;
            _debugImageAccentColors = value;
            OnPropertyChanged();
        }
    }

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

    public bool IsPopupMenuOpened
    {
        get => _isPopupMenuOpened;
        set
        {
            if (value == _isPopupMenuOpened) return;
            _isPopupMenuOpened = value;
            OnPropertyChanged();
        }
    }

    public List<FontFamily> FontFamilies { get; } =
        new (Fonts.SystemFontFamilies) { new FontFamily("/ClassIsland;component/Assets/Fonts/#HarmonyOS Sans SC") };

    public KeyValuePair<string, IMiniInfoProvider>? SelectedMiniInfoProvider
    {
        get => _selectedMiniInfoProvider;
        set
        {
            if (Equals(value, _selectedMiniInfoProvider)) return;
            _selectedMiniInfoProvider = value;
            OnPropertyChanged();
        }
    }

    public List<string> WeatherSampleList
    {
        get
        {
            var l = new List<string>();
            for (var i = 0; i < 36; i++)
            {
                l.Add(i.ToString());
            }

            l.Add("53");
            l.Add("99");
            return l;
        }
    }

    public List<City> CitySearchResults
    {
        get => _citySearchResults;
        set
        {
            if (Equals(value, _citySearchResults)) return;
            _citySearchResults = value;
            OnPropertyChanged();
        }
    }

    public object? WeatherNotificationControlTest
    {
        get => _weatherNotificationControlTest;
        set
        {
            if (Equals(value, _weatherNotificationControlTest)) return;
            _weatherNotificationControlTest = value;
            OnPropertyChanged();
        }
    }

    public string DiagnosticInfo
    {
        get => _diagnosticInfo;
        set
        {
            if (value == _diagnosticInfo) return;
            _diagnosticInfo = value;
            OnPropertyChanged();
        }
    }

    public bool IsMoreNotificationSettingsExpanded
    {
        get => _isMoreNotificationSettingsExpanded;
        set
        {
            if (value == _isMoreNotificationSettingsExpanded) return;
            _isMoreNotificationSettingsExpanded = value;
            OnPropertyChanged();
        }
    }

    public List<eVoice> EdgeVoices { get; } = 
        Edge_tts.GetVoice().FindAll(i => i.Locale.Contains("zh-CN"));

    public string TestSpeechText
    {
        get => _testSpeechText;
        set
        {
            if (value == _testSpeechText) return;
            _testSpeechText = value;
            OnPropertyChanged();
        }
    }

    public bool IsRestartRequired
    {
        get => _isRestartRequired;
        set
        {
            if (value == _isRestartRequired) return;
            _isRestartRequired = value;
            OnPropertyChanged();
        }
    }

    public bool IsRefreshingContributors
    {
        get => _isRefreshingContributors;
        set
        {
            if (value == _isRefreshingContributors) return;
            _isRefreshingContributors = value;
            OnPropertyChanged();
        }
    }
}