using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows;
using Avalonia.Media;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Core.Models.Ruleset;
using ClassIsland.Core.Models.Weather;
using ClassIsland.Helpers;
using ClassIsland.Shared;
using ClassIsland.Shared.Abstraction.Models;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Notification;
using ClassIsland.Models.AllContributors;
using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Octokit;

using WindowsShortcutFactory;

using File = System.IO.File;
using ClassIsland.Core.Models;
using ClassIsland.Core.Abstractions.Models.Speech;
using ClassIsland.Core.Services;
using ClassIsland.Shared.ComponentModels;

namespace ClassIsland.Models;

public class Settings : ObservableRecipient, ILessonControlSettings, INotificationSettings, IGlobalSpeechSettings
{
    private int _theme = 2;
    private bool _iscustomBackgroundColorEnabled = false;
    private Color _backgroundColor = Colors.Black;
    private Color _primaryColor = Colors.DeepSkyBlue;
    private Color _secondaryColor = Colors.Aquamarine;
    private DateTime _singleWeekStartTime =
        DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek); // 取周日
    //  DateTime.Today.AddDays(-(DateTime.Today.DayOfWeek switch { // 取周一
    //         DayOfWeek.Sunday => 6,
    //         _ => (int)DateTime.Today.DayOfWeek - 1
    //     }));
    private int _classPrepareNotifySeconds = 60;
    private bool _showDate = true;
    private bool _hideOnClass = false;
    private bool _isClassChangingNotificationEnabled = true;
    private bool _isClassPrepareNotificationEnabled = true;
    private int _windowDockingLocation = 1;
    private int _windowDockingOffsetX = 0;
    private int _windowDockingOffsetY = 0;
    private int _windowDockingMonitorIndex = 0;
    private int _windowLayer = 1;
    private bool _isMouseClickingEnabled = false;
    private bool _hideOnFullscreen = false;
    private bool _isClassOffNotificationEnabled = true;
    private ObservableCollection<string> _excludedFullscreenWindow = new()
    {
        "explorer"
    };
    private ObservableCollection<int> _multiWeekRotationOffset = [-1, -1, 0, 0, 0];
    private int _multiWeekRotationMaxCycle = 4;

    private bool _hideOnMaxWindow = false;
    private double _opacity = 0.5;
    private bool _isDebugEnabled = false;
    private string _selectedProfile = "Default.json";
    private bool _isMainWindowVisible = true;
    private bool _isWelcomeWindowShowed = false;
    private bool _isReportingEnabled = true;
    private Dictionary<string, string> _releaseChannels = new()
    {
    };

    private string _selectedChannel = "https://install.appcenter.ms/api/v0.1/apps/hellowrc/classisland/distribution_groups/public";
    private DateTime _lastCheckUpdateTime = DateTime.MinValue;
    private AppCenterReleaseInfo _lastCheckUpdateInfoCache = new();
    private UpdateStatus _lastUpdateStatus = UpdateStatus.UpToDate;
    private int _updateMode = 3;
    private bool _autoInstallUpdateNextStartup = true;
    private bool _isDebugOptionsEnabled = false;
    private Color _selectedPlatte = Colors.DodgerBlue;
    private int _selectedPlatteIndex = 0;
    private ObservableCollection<Color> _wallpaperColorPlatte = new(Enumerable.Repeat(Colors.DodgerBlue, 5));
    private int _colorSource = 1;
    private string _wallpaperClassName = "";
    private double _targetLightValue = 0.6;
    private double _scale = 1.0;
    private bool _isMainWindowDebugEnabled = false;
    private ObservableDictionary<string, bool> _notificationProvidersEnableStates = new();
    private ObservableCollection<string> _notificationProvidersPriority = new();
    private ObservableDictionary<string, object?> _notificationProvidersSettings = new();
    private bool _isNotificationEnabled = true;
    private bool _isWallpaperAutoUpdateEnabled = false;
    private int _wallpaperAutoUpdateIntervalSeconds = 60;
    private bool _isFallbackModeEnabled = true;
    private string _mainWindowFont = MainWindow.DefaultFontFamilyKey;
    private ObservableDictionary<string, object?> _miniInfoProviderSettings = new();
    private string? _selectedMiniInfoProvider = "d9fc55d6-8061-4c21-b521-6b0532ff735f";
    private WeatherInfo _lastWeatherInfo = new();
    private string _cityId = "weathercn:101010100";
    private string _cityName = "北京 (北京, 中国)";
    private int _mainWindowFontWeight2 = (int)FontWeight.Medium;
    private int _taskBarIconClickBehavior = 0;
    private bool _showExtraInfoOnTimePoint = true;
    private int _extraInfoType = 0;
    private bool _isCountdownEnabled = true;
    private int _countdownSeconds = 60;
    private int _extraInfo4ShowSecondsSeconds = 300;
    private int _defaultOnClassTimePointMinutes = 40;
    private int _defaultBreakingTimePointMinutes = 10;
    private double _debugAnimationScale = 1.0;
    private double _debugTimeSpeed = 1.0;
    private double _debugTimeOffsetSeconds = 0.0;
    private bool _expIsExcelImportEnabled = false;
    private int _timeLayoutEditorIndex = 1;
    private bool _isSplashEnabled = true;
    private string _splashCustomText = "";
    private string _splashCustomLogoSource = "";
    private bool _isDebugConsoleEnabled = false;
    private string _debugGitHubAuthKey = "";
    private Dictionary<string, SpeedTestResult> _speedTestResults = new();
    private bool _isAutoSelectUpgradeMirror = true;
    private DateTime _lastSpeedTest = DateTime.MinValue;
    private UpdateSourceKind _lastUpdateSourceKind = UpdateSourceKind.None;
    private string _updateReleaseInfo = "";
    private Version _updateVersion = new Version();
    private Release _lastCheckUpdateInfoCacheGitHub = new Release();
    private string _updateDownloadUrl = "";
    private DateTime _firstLaunchTime = DateTime.Now;
    private long _diagnosticStartupCount = 0;
    private int _diagnosticCrashCount = 0;
    private DateTime _diagnosticLastCrashTime = DateTime.MinValue;
    private int _diagnosticMemoryKillCount = 0;
    private DateTime _diagnosticLastMemoryKillTime = DateTime.Now;
    private bool _isSystemSpeechSystemExist = false;
    private bool _isNetworkConnect = false;
    private bool _isSpeechEnabled = true;
    private double _speechVolume = 1.0;
    private int _speechSource = 0;
    private string _edgeTtsVoiceName = "zh-CN-XiaoxiaoNeural";
    private string _exactTimeServer = "ntp.aliyun.com";
    private bool _isExactTimeEnabled = true;
    private double _timeOffsetSeconds = 0.0;
    private bool _isNotificationEffectEnabled = true;
    private ObservableDictionary<string, NotificationSettings> _notificationProvidersNotifySettings = new();
    private bool _isNotificationSoundEnabled = true;
    private string _notificationSoundPath = "";
    private bool _isTimeAutoAdjustEnabled = false;
    private double _timeAutoAdjustSeconds = 0.0;
    private DateTime _lastTimeAdjustDateTime = DateTime.Now;
    private bool _isNotificationTopmostEnabled = true;
    private double _notificationEffectRenderingScale = 1.0;
    private bool _isNotificationEffectRenderingScaleAutoSet = false;
    private AllContributorsRc _contributorsCache = new();
    private bool _allowNotificationSpeech = false;
    private bool _allowNotificationEffect = true;
    private bool _allowNotificationSound = false;
    private bool _allowNotificationTopmost = true;
    private string _updateArtifactHash = "";
    private ObservableCollection<string> _excludedWeatherAlerts = new();
    private string _currentComponentConfig = "Default";
    private bool _isAutomationEnabled = false;
    private bool _isAutomationWarningVisible = true;
    private string _currentAutomationConfig = "Default";
    private Version _lastAppVersion = new Version("0.0.0.0");
    private bool _showComponentsMigrateTip = false;
    private bool _expAllowEditingActivatedTimeLayout = false;
    private ObservableDictionary<string, string> _pluginIndexSelectedMirrors = new();
    private ObservableCollection<string> _userPluginIndexes = new();
    private ObservableDictionary<string, string> _additionalPluginIndexes = new();
    private ObservableCollection<PluginIndexInfo> _pluginIndexes = new();
    private string _officialSelectedMirror = "github";
    private ObservableDictionary<string, string> _officialIndexMirrors = new()
    {
        { "github", "https://github.com" },
        { "ghproxy", "https://mirror.ghproxy.com/https://github.com" },
        { "moeyy", "https://github.moeyy.xyz/https://github.com" }
    };

    private bool _isMigratedFromv14 = false;
    private DateTime _lastRefreshPluginSourceTime = DateTime.MinValue;
    private bool _isProfileEditorClassInfoSubjectAutoMoveNextEnabled = true;
    private double _notificationSoundVolume = 1.0;
    private double _radiusX = 0.0;
    private double _radiusY = 0.0;
    private int _hideMode = 0;
    private Ruleset _hiedRules = new();
    private bool _isAutoBackupEnabled = true;
    private DateTime _lastAutoBackupTime = DateTime.Now;
    private string _backupFilesSize = "计算中...";
    private int _autoBackupLimit = 16;
    private int _autoBackupIntervalDays = 7;
    private bool _useRawInput = false;
    private bool _isMouseInFadingEnabled = true;
    private bool _isMouseInFadingReversed = false;
    private double _touchInFadingDurationMs = 0;
    private bool _isCompatibleWindowTransparentEnabled = false;
    private double _mainWindowSecondaryFontSize = 14;
    private double _mainWindowBodyFontSize = 16;
    private double _mainWindowEmphasizedFontSize = 18;
    private double _mainWindowLargeFontSize = 20;
    private bool _isErrorLoadingRawInput = false;
    private bool _isCustomForegroundColorEnabled = false;
    private Color _customForegroundColor = Colors.DodgerBlue;
    private bool _isPluginMarketWarningVisible = true;
    private bool _isTransientDisabled = false;
    private bool _isWaitForTransientDisabled = false;
    private bool _isCriticalSafeMode = false;
    private double _scheduleSpacing = 1;
    private bool _showCurrentLessonOnlyOnClass = false;
    private bool _isSwapMode = true;
    private Dictionary<string, Dictionary<string, dynamic?>> _settingsOverlay = [];
    private bool _showEchoCaveWhenSettingsPageLoading = false;
    private int _settingsPagesCachePolicy = 0;
    private string _notificationSpeechCustomSmgTokenSource = "";

    private string _selectedUpdateMirrorV2 = "main";
    private string _selectedUpdateChannelV2 = "stable";
    private GptSoVitsSpeechSettings _gptSoVitsSpeechSettings = new();
    private double _mainWindowLineVerticalMargin = 5;
    private ObservableCollection<Guid> _trustedProfileIds = [];
    private bool _isNonExactCountdownEnabled = false;
    private bool _showDetailedStatusOnSplash = false;


    public void NotifyPropertyChanged(string propertyName)
    {
        OnPropertyChanged(propertyName);
    }

    public string SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (value == _selectedProfile) return;
            _selectedProfile = value;
            OnPropertyChanged();
        }
    }

    public bool IsMainWindowVisible
    {
        get => _isMainWindowVisible;
        set
        {
            if (value == _isMainWindowVisible) return;
            _isMainWindowVisible = value;
            OnPropertyChanged();
        }
    }

    public bool IsWelcomeWindowShowed
    {
        get => _isWelcomeWindowShowed;
        set
        {
            if (value == _isWelcomeWindowShowed) return;
            _isWelcomeWindowShowed = value;
            OnPropertyChanged();
        }
    }

    public Dictionary<string, Dictionary<string, dynamic?>> SettingsOverlay
    {
        get => _settingsOverlay;
        set
        {
            if (value == _settingsOverlay) return;
            _settingsOverlay = value;
            OnPropertyChanged();
        }
    }

    public double WeatherLongitude
    {
        get => _weatherLongitude;
        set
        {
            if (value.Equals(_weatherLongitude)) return;
            _weatherLongitude = value;
            OnPropertyChanged();
        }
    }

    public double WeatherLatitude
    {
        get => _weatherLatitude;
        set
        {
            if (value.Equals(_weatherLatitude)) return;
            _weatherLatitude = value;
            OnPropertyChanged();
        }
    }

    public int WeatherLocationSource
    {
        get => _weatherLocationSource;
        set
        {
            if (value == _weatherLocationSource) return;
            _weatherLocationSource = value;
            OnPropertyChanged();
        }
    }

    public bool AutoRefreshWeatherLocation
    {
        get => _autoRefreshWeatherLocation;
        set
        {
            if (value == _autoRefreshWeatherLocation) return;
            _autoRefreshWeatherLocation = value;
            OnPropertyChanged();
        }
    }

    #region Gerneral

    public DateTime SingleWeekStartTime
    {
        get => _singleWeekStartTime;
        set
        {
            if (value.Equals(_singleWeekStartTime)) return;
            _singleWeekStartTime = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<int> MultiWeekRotationOffset
    {
        get => _multiWeekRotationOffset;
        set
        {
            if (value.Equals(_multiWeekRotationOffset)) return;
            _multiWeekRotationOffset = value;
            OnPropertyChanged();
        }
    }

    public int MultiWeekRotationMaxCycle
    {
        get => _multiWeekRotationMaxCycle;
        set
        {
            if (value == _multiWeekRotationMaxCycle) return;
            _multiWeekRotationMaxCycle = value;
            OnPropertyChanged();
        }
    }

    public int ClassPrepareNotifySeconds
    {
        get => _classPrepareNotifySeconds;
        set
        {
            if (value == _classPrepareNotifySeconds) return;
            _classPrepareNotifySeconds = value;
            OnPropertyChanged();
        }
    }

    public bool IsClassPrepareNotificationEnabled
    {
        get => _isClassPrepareNotificationEnabled;
        set
        {
            if (value == _isClassPrepareNotificationEnabled) return;
            _isClassPrepareNotificationEnabled = value;
            OnPropertyChanged();
        }
    }

    public ObservableDictionary<string, object?> MiniInfoProviderSettings
    {
        get => _miniInfoProviderSettings;
        set
        {
            if (Equals(value, _miniInfoProviderSettings)) return;
            _miniInfoProviderSettings = value;
            OnPropertyChanged();
        }
    }

    public string? SelectedMiniInfoProvider
    {
        get => _selectedMiniInfoProvider;
        set
        {
            if (value == _selectedMiniInfoProvider) return;
            _selectedMiniInfoProvider = value;
            OnPropertyChanged();
        }
    }

    public bool ShowDate
    {
        get => _showDate;
        set
        {
            if (value == _showDate) return;
            _showDate = value;
            OnPropertyChanged();
        }
    }

    public bool HideOnClass
    {
        get => _hideOnClass;
        set
        {
            if (value == _hideOnClass) return;
            _hideOnClass = value;
            OnPropertyChanged();
        }
    }

    public bool IsClassChangingNotificationEnabled
    {
        get => _isClassChangingNotificationEnabled;
        set
        {
            if (value == _isClassChangingNotificationEnabled) return;
            _isClassChangingNotificationEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsClassOffNotificationEnabled
    {
        get => _isClassOffNotificationEnabled;
        set
        {
            if (value == _isClassOffNotificationEnabled) return;
            _isClassOffNotificationEnabled = value;
            OnPropertyChanged();
        }
    }

    public int HideMode
    {
        get => _hideMode;
        set
        {
            if (value == _hideMode) return;
            _hideMode = value;
            OnPropertyChanged();
        }
    }

    public Ruleset HiedRules
    {
        get => _hiedRules;
        set
        {
            if (Equals(value, _hiedRules)) return;
            _hiedRules = value;
            OnPropertyChanged();
        }
    }

    public bool HideOnFullscreen
    {
        get => _hideOnFullscreen;
        set
        {
            if (value == _hideOnFullscreen) return;
            _hideOnFullscreen = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> ExcludedFullscreenWindow
    {
        get => _excludedFullscreenWindow;
        set
        {
            if (Equals(value, _excludedFullscreenWindow)) return;
            _excludedFullscreenWindow = value;
            OnPropertyChanged();
        }
    }

    public bool HideOnMaxWindow
    {
        get => _hideOnMaxWindow;
        set
        {
            if (value == _hideOnMaxWindow) return;
            _hideOnMaxWindow = value;
            OnPropertyChanged();
        }
    }

    public bool IsReportingEnabled
    {
        get => _isReportingEnabled;
        set
        {
            if (value == _isReportingEnabled) return;
            _isReportingEnabled = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public bool IsSentryEnabled
    {
        get => GlobalStorageService.GetValue("IsSentryEnabled") is "1" or null;
        set
        {
            try
            {
                var envVar = value ? "1" : "0";
                GlobalStorageService.SetValue("IsSentryEnabled", envVar);
                OnPropertyChanged();
            }
            catch (Exception ex)
            {
                IAppHost.GetService<ILogger<Settings>>().LogError(ex, "无法设置 Sentry 启用状态。");
            }
        }
    }

    /// <summary>
    /// TaskBarIcon点击行为
    /// </summary>
    /// <value>
    /// <list type="bullet">
    ///     <item>0 - 打开主菜单</item>
    ///     <item>1 - 打开档案编辑窗口</item>
    ///     <item>2 - 显示/隐藏主界面</item>
    ///     <item>3 - 打开换课窗口</item>
    /// </list>
    /// </value>
    public int TaskBarIconClickBehavior
    {
        get => _taskBarIconClickBehavior;
        set
        {
            if (value == _taskBarIconClickBehavior) return;
            _taskBarIconClickBehavior = value;
            OnPropertyChanged();
        }
    }

    public bool ShowExtraInfoOnTimePoint
    {
        get => _showExtraInfoOnTimePoint;
        set
        {
            if (value == _showExtraInfoOnTimePoint) return;
            _showExtraInfoOnTimePoint = value;
            OnPropertyChanged();
        }
    }

    public int ExtraInfoType
    {
        get => _extraInfoType;
        set
        {
            if (value == _extraInfoType) return;
            _extraInfoType = value;
            OnPropertyChanged();
        }
    }

    public bool IsCountdownEnabled
    {
        get => _isCountdownEnabled;
        set
        {
            if (value == _isCountdownEnabled) return;
            _isCountdownEnabled = value;
            OnPropertyChanged();
        }
    }

    public int CountdownSeconds
    {
        get => _countdownSeconds;
        set
        {
            if (value == _countdownSeconds) return;
            _countdownSeconds = value;
            OnPropertyChanged();
        }
    }

    public int ExtraInfo4ShowSecondsSeconds
    {
        get => _extraInfo4ShowSecondsSeconds;
        set
        {
            if (value == _extraInfo4ShowSecondsSeconds) return;
            _extraInfo4ShowSecondsSeconds = value;
            OnPropertyChanged();
        }
    }

    public double ScheduleSpacing
    {
        get => _scheduleSpacing;
        set
        {
            if (value.Equals(_scheduleSpacing)) return;
            _scheduleSpacing = value;
            OnPropertyChanged();
        }
    }

    public bool ShowCurrentLessonOnlyOnClass
    {
        get => _showCurrentLessonOnlyOnClass;
        set
        {
            if (value == _showCurrentLessonOnlyOnClass) return;
            _showCurrentLessonOnlyOnClass = value;
            OnPropertyChanged();
        }
    }

    public bool IsNonExactCountdownEnabled
    {
        get => _isNonExactCountdownEnabled;
        set
        {
            if (value == _isNonExactCountdownEnabled) return;
            _isNonExactCountdownEnabled = value;
            OnPropertyChanged();
        }
    }

    public int DefaultOnClassTimePointMinutes
    {
        get => _defaultOnClassTimePointMinutes;
        set
        {
            if (value == _defaultOnClassTimePointMinutes) return;
            _defaultOnClassTimePointMinutes = value;
            OnPropertyChanged();
        }
    }

    public int DefaultBreakingTimePointMinutes
    {
        get => _defaultBreakingTimePointMinutes;
        set
        {
            if (value == _defaultBreakingTimePointMinutes) return;
            _defaultBreakingTimePointMinutes = value;
            OnPropertyChanged();
        }
    }

    public bool IsSplashEnabled
    {
        get => _isSplashEnabled;
        set
        {
            if (value == _isSplashEnabled) return;
            _isSplashEnabled = value;
            OnPropertyChanged();
        }
    }

    public string SplashCustomText
    {
        get => _splashCustomText;
        set
        {
            if (value == _splashCustomText) return;
            _splashCustomText = value;
            OnPropertyChanged();
        }
    }

    public string SplashCustomLogoSource
    {
        get => _splashCustomLogoSource;
        set
        {
            if (value == _splashCustomLogoSource) return;
            _splashCustomLogoSource = value;
            OnPropertyChanged();
        }
    }

    public string ExactTimeServer
    {
        get => _exactTimeServer;
        set
        {
            if (value == _exactTimeServer) return;
            _exactTimeServer = value;
            OnPropertyChanged();
        }
    }

    public bool IsExactTimeEnabled
    {
        get => _isExactTimeEnabled;
        set
        {
            if (value == _isExactTimeEnabled) return;
            _isExactTimeEnabled = value;
            OnPropertyChanged();
        }
    }

    public double TimeOffsetSeconds
    {
        get => _timeOffsetSeconds;
        set
        {
            if (value.Equals(_timeOffsetSeconds)) return;
            _timeOffsetSeconds = value;
            OnPropertyChanged();
        }
    }

    public bool IsTimeAutoAdjustEnabled
    {
        get => _isTimeAutoAdjustEnabled;
        set
        {
            if (value == _isTimeAutoAdjustEnabled) return;
            _isTimeAutoAdjustEnabled = value;
            OnPropertyChanged();
        }
    }

    public double TimeAutoAdjustSeconds
    {
        get => _timeAutoAdjustSeconds;
        set
        {
            if (value.Equals(_timeAutoAdjustSeconds)) return;
            _timeAutoAdjustSeconds = value;
            OnPropertyChanged();
        }
    }

    public DateTime LastTimeAdjustDateTime
    {
        get => _lastTimeAdjustDateTime;
        set
        {
            if (value.Equals(_lastTimeAdjustDateTime)) return;
            _lastTimeAdjustDateTime = value;
            OnPropertyChanged();
        }
    }

    public bool IsWaitForTransientDisabled
    {
        get => _isWaitForTransientDisabled;
        set
        {
            if (value == _isWaitForTransientDisabled) return;
            _isWaitForTransientDisabled = value;
            OnPropertyChanged();
        }
    }

    public int AnimationLevel
    {
        get => _animationLevel;
        set
        {
            if (value == _animationLevel) return;
            _animationLevel = value;
            OnPropertyChanged();
        }
    }

    public bool IsCriticalSafeMode
    {
        get => _isCriticalSafeMode;
        set
        {
            if (value == _isCriticalSafeMode) return;
            _isCriticalSafeMode = value;
            OnPropertyChanged();
        }
    }

    public bool ShowDetailedStatusOnSplash
    {
        get => _showDetailedStatusOnSplash;
        set
        {
            if (value == _showDetailedStatusOnSplash) return;
            _showDetailedStatusOnSplash = value;
            OnPropertyChanged();
        }
    }

    public int CriticalSafeModeMethod
    {
        get => _criticalSafeModeMethod;
        set
        {
            if (value == _criticalSafeModeMethod) return;
            _criticalSafeModeMethod = value;
            OnPropertyChanged();
        }
    }

    public bool AutoDisableCorruptPlugins
    {
        get => _autoDisableCorruptPlugins;
        set
        {
            if (value == _autoDisableCorruptPlugins) return;
            _autoDisableCorruptPlugins = value;
            OnPropertyChanged();
        }
    }

    public bool CorruptPluginsDisabledLastSession
    {
        get => _corruptPluginsDisabledLastSession;
        set
        {
            if (value == _corruptPluginsDisabledLastSession) return;
            _corruptPluginsDisabledLastSession = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Appearence

    public int Theme
    {
        get => _theme;
        set
        {
            if (value == _theme) return;
            _theme = value;
            OnPropertyChanged();
        }
    }

    public Color PrimaryColor
    {
        get => _primaryColor;
        set
        {
            if (value.Equals(_primaryColor)) return;
            _primaryColor = value;
            OnPropertyChanged();
        }
    }

    public Color SecondaryColor
    {
        get => _secondaryColor;
        set
        {
            if (value.Equals(_secondaryColor)) return;
            _secondaryColor = value;
            OnPropertyChanged();
        }
    }

    public int ColorSource
    {
        get => _colorSource;
        set
        {
            if (value == _colorSource) return;
            _colorSource = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Color> WallpaperColorPlatte
    {
        get => _wallpaperColorPlatte;
        set
        {
            if (Equals(value, _wallpaperColorPlatte)) return;
            _wallpaperColorPlatte = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public Color SelectedPlatte => WallpaperColorPlatte.Count < Math.Max(SelectedPlatteIndex, 0) + 1 
        ? Colors.DodgerBlue 
        : WallpaperColorPlatte[SelectedPlatteIndex];

    public int SelectedPlatteIndex
    {
        get => _selectedPlatteIndex;
        set
        {
            if (value == _selectedPlatteIndex) return;
            _selectedPlatteIndex = value;
            OnPropertyChanged();
        }
    }

    public bool IsWallpaperAutoUpdateEnabled
    {
        get => _isWallpaperAutoUpdateEnabled;
        set
        {
            if (value == _isWallpaperAutoUpdateEnabled) return;
            _isWallpaperAutoUpdateEnabled = value;
            OnPropertyChanged();
        }
    }

    public int WallpaperAutoUpdateIntervalSeconds
    {
        get => _wallpaperAutoUpdateIntervalSeconds;
        set
        {
            if (value == _wallpaperAutoUpdateIntervalSeconds) return;
            _wallpaperAutoUpdateIntervalSeconds = value;
            OnPropertyChanged();
        }
    }

    public string WallpaperClassName
    {
        get => _wallpaperClassName;
        set
        {
            if (value == _wallpaperClassName) return;
            _wallpaperClassName = value;
            OnPropertyChanged();
        }
    }

    public bool IsFallbackModeEnabled
    {
        get => _isFallbackModeEnabled;
        set
        {
            if (value == _isFallbackModeEnabled) return;
            _isFallbackModeEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool UseExperimentColorPickingMethod
    {
        get => _useExperimentColorPickingMethod;
        set
        {
            if (value == _useExperimentColorPickingMethod) return;
            _useExperimentColorPickingMethod = value;
            OnPropertyChanged();
        }
    }

    public double TargetLightValue
    {
        get => _targetLightValue;
        set
        {
            if (value.Equals(_targetLightValue)) return;
            _targetLightValue = value;
            OnPropertyChanged();
        }
    }

    public bool IsCustomBackgroundColorEnabled
    {
        get => _iscustomBackgroundColorEnabled;
        set
        {
            if (value == _iscustomBackgroundColorEnabled) return;
            _iscustomBackgroundColorEnabled = value;
            OnPropertyChanged();
        }
    }

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if (value.Equals(_backgroundColor)) return;
            _backgroundColor = value;
            OnPropertyChanged();
        }
    }

    public double Opacity
    {
        get => _opacity;
        set
        {
            if (value.Equals(_opacity)) return;
            _opacity = value;
            OnPropertyChanged();
        }
    }

    public double Scale
    {
        get => _scale;
        set
        {
            if (value.Equals(_scale)) return;
            _scale = value;
            OnPropertyChanged();
        }
    }

    public string MainWindowFont
    {
        get => _mainWindowFont;
        set
        {
            if (value == _mainWindowFont) return;
            _mainWindowFont = value;
            OnPropertyChanged();
        }
    }

    public int MainWindowFontWeight2
    {
        get => _mainWindowFontWeight2;
        set
        {
            if (value.Equals(_mainWindowFontWeight2)) return;
            _mainWindowFontWeight2 = value;
            OnPropertyChanged();
        }
    }

    public double RadiusX
    {
        get => _radiusX;
        set
        {
            if (value.Equals(_radiusX)) return;
            _radiusX = value;
            OnPropertyChanged();
        }
    }

    public double RadiusY
    {
        get => _radiusY;
        set
        {
            if (value.Equals(_radiusY)) return;
            _radiusY = value;
            OnPropertyChanged();
        }
    }

    public double MainWindowSecondaryFontSize
    {
        get => _mainWindowSecondaryFontSize;
        set
        {
            if (value.Equals(_mainWindowSecondaryFontSize)) return;
            _mainWindowSecondaryFontSize = value;
            OnPropertyChanged();
        }
    }

    public double MainWindowBodyFontSize
    {
        get => _mainWindowBodyFontSize;
        set
        {
            if (value.Equals(_mainWindowBodyFontSize)) return;
            _mainWindowBodyFontSize = value;
            OnPropertyChanged();
        }
    }

    public double MainWindowEmphasizedFontSize
    {
        get => _mainWindowEmphasizedFontSize;
        set
        {
            if (value.Equals(_mainWindowEmphasizedFontSize)) return;
            _mainWindowEmphasizedFontSize = value;
            OnPropertyChanged();
        }
    }

    public double MainWindowLargeFontSize
    {
        get => _mainWindowLargeFontSize;
        set
        {
            if (value.Equals(_mainWindowLargeFontSize)) return;
            _mainWindowLargeFontSize = value;
            OnPropertyChanged();
        }
    }

    public bool IsCustomForegroundColorEnabled
    {
        get => _isCustomForegroundColorEnabled;
        set
        {
            if (value == _isCustomForegroundColorEnabled) return;
            _isCustomForegroundColorEnabled = value;
            OnPropertyChanged();
        }
    }

    public Color CustomForegroundColor
    {
        get => _customForegroundColor;
        set
        {
            if (value.Equals(_customForegroundColor)) return;
            _customForegroundColor = value;
            OnPropertyChanged();
        }
    }

    public double MainWindowLineVerticalMargin
    {
        get => _mainWindowLineVerticalMargin;
        set
        {
            if (value.Equals(_mainWindowLineVerticalMargin)) return;
            _mainWindowLineVerticalMargin = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Components

    public string CurrentComponentConfig
    {
        get => _currentComponentConfig;
        set
        {
            if (value == _currentComponentConfig) return;
            _currentComponentConfig = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Notifications

    public bool IsNotificationEnabled
    {
        get => _isNotificationEnabled;
        set
        {
            if (value == _isNotificationEnabled) return;
            _isNotificationEnabled = value;
            OnPropertyChanged();
        }
    }

    public ObservableDictionary<string, bool> NotificationProvidersEnableStates
    {
        get => _notificationProvidersEnableStates;
        set
        {
            if (Equals(value, _notificationProvidersEnableStates)) return;
            _notificationProvidersEnableStates = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> NotificationProvidersPriority
    {
        get => _notificationProvidersPriority;
        set
        {
            if (Equals(value, _notificationProvidersPriority)) return;
            _notificationProvidersPriority = value;
            OnPropertyChanged();
        }
    }

    public ObservableDictionary<string, object?> NotificationProvidersSettings
    {
        get => _notificationProvidersSettings;
        set
        {
            if (Equals(value, _notificationProvidersSettings)) return;
            _notificationProvidersSettings = value;
            OnPropertyChanged();
        }
    }

    public ObservableDictionary<string, NotificationSettings> NotificationProvidersNotifySettings
    {
        get => _notificationProvidersNotifySettings;
        set
        {
            if (Equals(value, _notificationProvidersNotifySettings)) return;
            _notificationProvidersNotifySettings = value;
            OnPropertyChanged();
        }
    }

    public ObservableDictionary<string, NotificationSettings> NotificationChannelsNotifySettings
    {
        get => _notificationChannelsNotifySettings;
        set
        {
            if (Equals(value, _notificationChannelsNotifySettings)) return;
            _notificationChannelsNotifySettings = value;
            OnPropertyChanged();
        }
    }

    public bool IsSystemSpeechSystemExist
    {
        get => _isSystemSpeechSystemExist;
        set
        {
            if (value == _isSystemSpeechSystemExist) return;
            _isSystemSpeechSystemExist = value;
            OnPropertyChanged();
        }
    }

    public bool IsNetworkConnect
    {
        get => _isNetworkConnect;
        set
        {
            if (value == _isNetworkConnect) return;
            _isNetworkConnect = value;
            OnPropertyChanged();
        }
    }

    public bool IsSpeechEnabled
    {
        get => _isSpeechEnabled;
        set
        {
            if (value == _isSpeechEnabled) return;
            _isSpeechEnabled = value;
            OnPropertyChanged();
        }
    }

    public double SpeechVolume
    {
        get => _speechVolume;
        set
        {
            if (value.Equals(_speechVolume)) return;
            _speechVolume = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 语音合成源
    /// </summary>
    /// <value>
    /// 0 - 系统TTS<br/>
    /// 1 - EdgeTTS
    /// </value>
    public int SpeechSource
    {
        get => _speechSource;
        set
        {
            if (value == _speechSource) return;
            if (!IsSystemSpeechSystemExist)
            {
                _speechSource = 1;
                OnPropertyChanged();
                return;
            }
            _speechSource = value;
            OnPropertyChanged();
        }
    }

    public string SelectedSpeechProvider
    {
        get => _selectedSpeechProvider;
        set
        {
            if (value == _selectedSpeechProvider) return;
            _selectedSpeechProvider = value;
            OnPropertyChanged();
        }
    }

    public string EdgeTtsVoiceName
    {
        get => _edgeTtsVoiceName;
        set
        {
            if (value == _edgeTtsVoiceName) return;
            _edgeTtsVoiceName = value;
            OnPropertyChanged();
        }
    }

    public bool IsNotificationEffectEnabled
    {
        get => _isNotificationEffectEnabled;
        set
        {
            if (value == _isNotificationEffectEnabled) return;
            _isNotificationEffectEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsNotificationSoundEnabled
    {
        get => _isNotificationSoundEnabled;
        set
        {
            if (value == _isNotificationSoundEnabled) return;
            _isNotificationSoundEnabled = value;
            OnPropertyChanged();
        }
    }

    public string NotificationSoundPath
    {
        get => _notificationSoundPath;
        set
        {
            if (value == _notificationSoundPath) return;
            _notificationSoundPath = value;
            OnPropertyChanged();
        }
    }

    public bool IsNotificationTopmostEnabled
    {
        get => _isNotificationTopmostEnabled;
        set
        {
            if (value == _isNotificationTopmostEnabled) return;
            _isNotificationTopmostEnabled = value;
            OnPropertyChanged();
        }
    }

    public double NotificationEffectRenderingScale
    {
        get => _notificationEffectRenderingScale;
        set
        {
            if (value.Equals(_notificationEffectRenderingScale)) return;
            _notificationEffectRenderingScale = value;
            OnPropertyChanged();
        }
    }

    public bool IsNotificationEffectRenderingScaleAutoSet
    {
        get => _isNotificationEffectRenderingScaleAutoSet;
        set
        {
            if (value == _isNotificationEffectRenderingScaleAutoSet) return;
            _isNotificationEffectRenderingScaleAutoSet = value;
            OnPropertyChanged();
        }
    }

    public bool AllowNotificationSpeech
    {
        get => _allowNotificationSpeech;
        set
        {
            if (value == _allowNotificationSpeech) return;
            _allowNotificationSpeech = value;
            OnPropertyChanged();
        }
    }

    public bool AllowNotificationEffect
    {
        get => _allowNotificationEffect;
        set
        {
            if (value == _allowNotificationEffect) return;
            _allowNotificationEffect = value;
            OnPropertyChanged();
        }
    }

    public bool AllowNotificationSound
    {
        get => _allowNotificationSound;
        set
        {
            if (value == _allowNotificationSound) return;
            _allowNotificationSound = value;
            OnPropertyChanged();
        }
    }

    public bool AllowNotificationTopmost
    {
        get => _allowNotificationTopmost;
        set
        {
            if (value == _allowNotificationTopmost) return;
            _allowNotificationTopmost = value;
            OnPropertyChanged();
        }
    }

    public double NotificationSoundVolume
    {
        get => _notificationSoundVolume;
        set
        {
            if (value.Equals(_notificationSoundVolume)) return;
            _notificationSoundVolume = value;
            OnPropertyChanged();
        }
    }

    public string NotificationSpeechCustomSmgTokenSource
    {
        get => _notificationSpeechCustomSmgTokenSource;
        set
        {
            if (value == _notificationSpeechCustomSmgTokenSource) return;
            _notificationSpeechCustomSmgTokenSource = value;
            OnPropertyChanged();
        }
    }

    public GptSoVitsSpeechSettings GptSoVitsSpeechSettings
    {
        get => _gptSoVitsSpeechSettings;
        set
        {
            if (Equals(value, _gptSoVitsSpeechSettings)) return;
            _gptSoVitsSpeechSettings = value;
            OnPropertyChanged();
        }
    }

    public bool NotificationUseStandaloneEffectUiThread
    {
        get => _notificationUseStandaloneEffectUiThread;
        set
        {
            if (value == _notificationUseStandaloneEffectUiThread) return;
            _notificationUseStandaloneEffectUiThread = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Automations

    public bool IsAutomationEnabled
    {
        get => _isAutomationEnabled;
        set
        {
            if (value == _isAutomationEnabled) return;
            _isAutomationEnabled = value;
            OnPropertyChanged();
        }
    }

    public string CurrentAutomationConfig
    {
        get => _currentAutomationConfig;
        set
        {
            if (value == _currentAutomationConfig) return;
            _currentAutomationConfig = value;
            OnPropertyChanged();
        }
    }

    public bool IsAutomationWarningVisible
    {
        get => _isAutomationWarningVisible;
        set
        {
            if (value == _isAutomationWarningVisible) return;
            _isAutomationWarningVisible = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region AppUpgrades

    /// <summary>
    /// 更新模式
    /// </summary>
    /// 
    public int UpdateMode
    {
        get => _updateMode;
        set
        {
            if (value == _updateMode) return;
            _updateMode = value;
            OnPropertyChanged();
        }
    }

    [Obsolete]
    public string SelectedChannel
    {
        get => _selectedChannel;
        set
        {
            if (value == _selectedChannel) return;
            _selectedChannel = value;
            OnPropertyChanged();
        }
    }

    public DateTime LastCheckUpdateTime
    {
        get => _lastCheckUpdateTime;
        set
        {
            if (value.Equals(_lastCheckUpdateTime)) return;
            _lastCheckUpdateTime = value;
            OnPropertyChanged();
        }
    }


    public UpdateStatus LastUpdateStatus
    {
        get => _lastUpdateStatus;
        set
        {
            if (value == _lastUpdateStatus) return;
            _lastUpdateStatus = value;
            OnPropertyChanged();
        }
    }

    public bool AutoInstallUpdateNextStartup
    {
        get => _autoInstallUpdateNextStartup;
        set
        {
            if (value == _autoInstallUpdateNextStartup) return;
            _autoInstallUpdateNextStartup = value;
            OnPropertyChanged();
        }
    }

    public Dictionary<string, SpeedTestResult> SpeedTestResults
    {
        get => _speedTestResults;
        set
        {
            if (Equals(value, _speedTestResults)) return;
            _speedTestResults = value;
            OnPropertyChanged();
        }
    }


    public bool IsAutoSelectUpgradeMirror
    {
        get => _isAutoSelectUpgradeMirror;
        set
        {
            if (value == _isAutoSelectUpgradeMirror) return;
            _isAutoSelectUpgradeMirror = value;
            OnPropertyChanged();
        }
    }

    public DateTime LastSpeedTest
    {
        get => _lastSpeedTest;
        set
        {
            if (value.Equals(_lastSpeedTest)) return;
            _lastSpeedTest = value;
            OnPropertyChanged();
        }
    }

    public UpdateSourceKind LastUpdateSourceKind
    {
        get => _lastUpdateSourceKind;
        set
        {
            if (value == _lastUpdateSourceKind) return;
            _lastUpdateSourceKind = value;
            OnPropertyChanged();
        }
    }

    public string UpdateReleaseInfo
    {
        get => _updateReleaseInfo;
        set
        {
            if (value == _updateReleaseInfo) return;
            _updateReleaseInfo = value;
            OnPropertyChanged();
        }
    }


    public string UpdateDownloadUrl
    {
        get => _updateDownloadUrl;
        set
        {
            if (value == _updateDownloadUrl) return;
            _updateDownloadUrl = value;
            OnPropertyChanged();
        }
    }

    public Version UpdateVersion
    {
        get => _updateVersion;
        set
        {
            if (Equals(value, _updateVersion)) return;
            _updateVersion = value;
            OnPropertyChanged();
        }
    }

    public string UpdateArtifactHash
    {
        get => _updateArtifactHash;
        set
        {
            if (value == _updateArtifactHash) return;
            _updateArtifactHash = value;
            OnPropertyChanged();
        }
    }

    public string SelectedUpdateMirrorV2
    {
        get => _selectedUpdateMirrorV2;
        set
        {
            if (value == _selectedUpdateMirrorV2) return;
            _selectedUpdateMirrorV2 = value;
            OnPropertyChanged();
        }
    }

    public string SelectedUpdateChannelV2
    {
        get => _selectedUpdateChannelV2;
        set
        {
            if (value == _selectedUpdateChannelV2) return;
            _selectedUpdateChannelV2 = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Window

    /// <summary>
    /// 窗口停靠位置
    /// 
    /// </summary>
    /// <value>
    /// <code>
    /// #############
    /// # 0   1   2 #
    /// #           #
    /// # 3   4   5 #
    /// #############
    /// </code>
    /// </value>
    public int WindowDockingLocation
    {
        get => _windowDockingLocation;
        set
        {
            if (value == _windowDockingLocation) return;
            _windowDockingLocation = value;
            OnPropertyChanged();
        }
    }

    bool _isIgnoreWorkAreaEnabled;
    private int _criticalSafeModeMethod = 0;
    private bool _notificationUseStandaloneEffectUiThread = true;
    private double _weatherLongitude = 0.0;
    private double _weatherLatitude = 0.0;
    private int _weatherLocationSource = 0;
    private bool _autoRefreshWeatherLocation = false;
    private bool _useExperimentColorPickingMethod = false;
    private bool _autoDisableCorruptPlugins = true;
    private bool _corruptPluginsDisabledLastSession = false;
    private ObservableDictionary<string, NotificationSettings> _notificationChannelsNotifySettings = new();
    private string _selectedSpeechProvider = "classisland.speech.edgeTts";
    private bool _isThemeWarningVisible = true;
    private string _weatherIconId = "classisland.weatherIcons.fluentDesign";
    private bool _isRollingComponentWarningVisible = true;
    private int _animationLevel = 1;

    public bool IsIgnoreWorkAreaEnabled
    {
        get => _isIgnoreWorkAreaEnabled;
        set
        {
            if (value == _isIgnoreWorkAreaEnabled) return;
            _isIgnoreWorkAreaEnabled = value;
            OnPropertyChanged();
        }
    }
    
    public int WindowDockingOffsetX
    {
        get => _windowDockingOffsetX;
        set
        {
            if (value == _windowDockingOffsetX) return;
            _windowDockingOffsetX = value;
            OnPropertyChanged();
        }
    }

    public int WindowDockingOffsetY
    {
        get => _windowDockingOffsetY;
        set
        {
            if (value == _windowDockingOffsetY) return;
            _windowDockingOffsetY = value;
            OnPropertyChanged();
        }
    }

    public int WindowDockingMonitorIndex
    {
        get => _windowDockingMonitorIndex;
        set
        {
            if (value == _windowDockingMonitorIndex) return;
            

            if (value < 0)
            {
                throw new ArgumentException("选择不能为空。");
            }
            _windowDockingMonitorIndex = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 窗口层级
    /// </summary>
    /// <value>
    /// 0 - 置底<br/>
    /// 1 - 置顶
    /// </value>
    public int WindowLayer
    {
        get => _windowLayer;
        set
        {
            if (value == _windowLayer) return;
            _windowLayer = value;
            OnPropertyChanged();
        }
    }

    public bool IsMouseClickingEnabled
    {
        get => _isMouseClickingEnabled;
        set
        {
            if (value == _isMouseClickingEnabled) return;
            _isMouseClickingEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool UseRawInput
    {
        get => _useRawInput;
        set
        {
            if (value == _useRawInput) return;
            _useRawInput = value;
            OnPropertyChanged();
        }
    }

    public bool IsMouseInFadingEnabled
    {
        get => _isMouseInFadingEnabled;
        set
        {
            if (value == _isMouseInFadingEnabled) return;
            _isMouseInFadingEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsMouseInFadingReversed
    {
        get => _isMouseInFadingReversed;
        set
        {
            if (value == _isMouseInFadingReversed) return;
            _isMouseInFadingReversed = value;
            OnPropertyChanged();
        }
    }

    public double TouchInFadingDurationMs
    {
        get => _touchInFadingDurationMs;
        set
        {
            if (value.Equals(_touchInFadingDurationMs)) return;
            _touchInFadingDurationMs = value;
            OnPropertyChanged();
        }
    }

    public bool IsCompatibleWindowTransparentEnabled
    {
        get => _isCompatibleWindowTransparentEnabled;
        set
        {
            if (value == _isCompatibleWindowTransparentEnabled) return;
            _isCompatibleWindowTransparentEnabled = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public bool IsErrorLoadingRawInput
    {
        get => _isErrorLoadingRawInput;
        set
        {
            if (value == _isErrorLoadingRawInput) return;
            _isErrorLoadingRawInput = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Weather

    public WeatherInfo LastWeatherInfo
    {
        get => _lastWeatherInfo;
        set
        {
            if (Equals(value, _lastWeatherInfo)) return;
            _lastWeatherInfo = value;
            OnPropertyChanged();
        }
    }

    public string CityId
    {
        get => _cityId;
        set
        {
            if (value == _cityId || value == null) return;
            _cityId = value;
            OnPropertyChanged();
        }
    }

    public string CityName
    {
        get => _cityName;
        set
        {
            if (value == _cityName) return;
            _cityName = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> ExcludedWeatherAlerts
    {
        get => _excludedWeatherAlerts;
        set
        {
            if (Equals(value, _excludedWeatherAlerts)) return;
            _excludedWeatherAlerts = value;
            OnPropertyChanged();
        }
    }

    public string WeatherIconId
    {
        get => _weatherIconId;
        set
        {
            if (value == _weatherIconId) return;
            _weatherIconId = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Exp

    [Obsolete]
    public bool ExpIsExcelImportEnabled
    {
        get => _expIsExcelImportEnabled;
        set
        {
            if (value == _expIsExcelImportEnabled) return;
            _expIsExcelImportEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool ExpAllowEditingActivatedTimeLayout
    {
        get => _expAllowEditingActivatedTimeLayout;
        set
        {
            if (value == _expAllowEditingActivatedTimeLayout) return;
            _expAllowEditingActivatedTimeLayout = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Diagnose

    public DateTime DiagnosticFirstLaunchTime
    {
        get => _firstLaunchTime;
        set
        {
            if (value.Equals(_firstLaunchTime)) return;
            _firstLaunchTime = value;
            OnPropertyChanged();
        }
    }

    public long DiagnosticStartupCount
    {
        get => _diagnosticStartupCount;
        set
        {
            if (value == _diagnosticStartupCount) return;
            _diagnosticStartupCount = value;
            OnPropertyChanged();
        }
    }

    public int DiagnosticCrashCount
    {
        get => _diagnosticCrashCount;
        set
        {
            if (value == _diagnosticCrashCount) return;
            _diagnosticCrashCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DiagnosticCrashFreqDay));
        }
    }

    public DateTime DiagnosticLastCrashTime
    {
        get => _diagnosticLastCrashTime;
        set
        {
            if (value.Equals(_diagnosticLastCrashTime)) return;
            _diagnosticLastCrashTime = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DiagnosticCrashFreqDay));
        }
    }

    [JsonIgnore]
    public double DiagnosticCrashFreqDay => DiagnosticCrashCount == 0
        ? 0
        : (DiagnosticLastCrashTime - DiagnosticFirstLaunchTime).TotalSeconds / 86400.0 * 1.0 / DiagnosticCrashCount;

    public int DiagnosticMemoryKillCount
    {
        get => _diagnosticMemoryKillCount;
        set
        {
            if (value == _diagnosticMemoryKillCount) return;
            _diagnosticMemoryKillCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DiagnosticMemoryKillFreqDay));
        }
    }

    public DateTime DiagnosticLastMemoryKillTime
    {
        get => _diagnosticLastMemoryKillTime;
        set
        {
            if (value.Equals(_diagnosticLastMemoryKillTime)) return;
            _diagnosticLastMemoryKillTime = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DiagnosticMemoryKillFreqDay));
        }
    }

    [JsonIgnore]
    public double DiagnosticMemoryKillFreqDay => DiagnosticMemoryKillCount == 0
        ? 0
        : (DiagnosticLastMemoryKillTime - DiagnosticFirstLaunchTime).TotalSeconds / 86400.0 * 1.0 / DiagnosticMemoryKillCount;

    #endregion

    #region Storage

    public bool IsAutoBackupEnabled
    {
        get => _isAutoBackupEnabled;
        set
        {
            if (value == _isAutoBackupEnabled) return;
            _isAutoBackupEnabled = value;
            OnPropertyChanged();
        }
    }

    public DateTime LastAutoBackupTime
    {
        get => _lastAutoBackupTime;
        set
        {
            if (value.Equals(_lastAutoBackupTime)) return;
            _lastAutoBackupTime = value;
            OnPropertyChanged();
        }
    }
    public string BackupFilesSize
    {
        get => _backupFilesSize;
        set
        {
            if (value == _backupFilesSize) return;
            _backupFilesSize = value;
            OnPropertyChanged();
        }
    }

    public int AutoBackupLimit
    {
        get => _autoBackupLimit;
        set
        {
            if (value == _autoBackupLimit) return;
            _autoBackupLimit = value;
            OnPropertyChanged();
        }
    }

    public int AutoBackupIntervalDays
    {
        get => _autoBackupIntervalDays;
        set
        {
            if (value == _autoBackupIntervalDays) return;
            _autoBackupIntervalDays = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Plugins

    public ObservableDictionary<string, string> OfficialIndexMirrors
    {
        get => _officialIndexMirrors;
        set
        {
            if (Equals(value, _officialIndexMirrors)) return;
            _officialIndexMirrors = value;
            OnPropertyChanged();
        }
    }

    public string OfficialSelectedMirror
    {
        get => _officialSelectedMirror;
        set
        {
            if (value == _officialSelectedMirror) return;
            _officialSelectedMirror = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<PluginIndexInfo> PluginIndexes
    {
        get => _pluginIndexes;
        set
        {
            if (Equals(value, _pluginIndexes)) return;
            _pluginIndexes = value;
            OnPropertyChanged();
        }
    }

    public DateTime LastRefreshPluginSourceTime
    {
        get => _lastRefreshPluginSourceTime;
        set
        {
            if (value.Equals(_lastRefreshPluginSourceTime)) return;
            _lastRefreshPluginSourceTime = value;
            OnPropertyChanged();
        }
    }

    public bool IsPluginMarketWarningVisible
    {
        get => _isPluginMarketWarningVisible;
        set
        {
            if (value == _isPluginMarketWarningVisible) return;
            _isPluginMarketWarningVisible = value;
            OnPropertyChanged();
        }
    }

    #endregion

    public bool IsRollingComponentWarningVisible
    {
        get => _isRollingComponentWarningVisible;
        set
        {
            if (value == _isRollingComponentWarningVisible) return;
            _isRollingComponentWarningVisible = value;
            OnPropertyChanged();
        }
    }

    public bool IsThemeWarningVisible
    {
        get => _isThemeWarningVisible;
        set
        {
            if (value == _isThemeWarningVisible) return;
            _isThemeWarningVisible = value;
            OnPropertyChanged();
        }
    }

    public bool IsDebugEnabled
    {
        get => _isDebugEnabled;
        set
        {
            if (value == _isDebugEnabled) return;
            _isDebugEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsDebugOptionsEnabled
    {
        get => _isDebugOptionsEnabled;
        set
        {
            if (value == _isDebugOptionsEnabled) return;
            _isDebugOptionsEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsMainWindowDebugEnabled
    {
        get => _isMainWindowDebugEnabled;
        set
        {
            if (value == _isMainWindowDebugEnabled) return;
            _isMainWindowDebugEnabled = value;
            OnPropertyChanged();
        }
    }

    public double DebugAnimationScale
    {
        get => _debugAnimationScale;
        set
        {
            if (value.Equals(_debugAnimationScale)) return;
            _debugAnimationScale = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public double DebugTimeSpeed
    {
        get => _debugTimeSpeed;
        set
        {
            if (value.Equals(_debugTimeSpeed)) return;
            _debugTimeSpeed = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public double DebugTimeOffsetSeconds
    {
        get => _debugTimeOffsetSeconds;
        set
        {
            if (value == _debugTimeOffsetSeconds) return;
            _debugTimeOffsetSeconds = value;
            OnPropertyChanged();
        }
    }

    public int TimeLayoutEditorIndex
    {
        get => _timeLayoutEditorIndex;
        set
        {
            if (value == _timeLayoutEditorIndex) return;
            _timeLayoutEditorIndex = value;
            OnPropertyChanged();
        }
    }

    public bool IsDebugConsoleEnabled
    {
        get => _isDebugConsoleEnabled;
        set
        {
            if (value == _isDebugConsoleEnabled) return;
            _isDebugConsoleEnabled = value;
            OnPropertyChanged();
        }
    }

    public string DebugGitHubAuthKey
    {
        get => _debugGitHubAuthKey;
        set
        {
            if (value == _debugGitHubAuthKey) return;
            _debugGitHubAuthKey = value;
            OnPropertyChanged();
        }
    }

    public AllContributorsRc ContributorsCache
    {
        get => _contributorsCache;
        set
        {
            if (Equals(value, _contributorsCache)) return;
            _contributorsCache = value;
            OnPropertyChanged();
        }
    }

    public Version LastAppVersion
    {
        get => _lastAppVersion;
        set
        {
            if (Equals(value, _lastAppVersion)) return;
            _lastAppVersion = value;
            OnPropertyChanged();
        }
    }

    public bool ShowComponentsMigrateTip
    {
        get => _showComponentsMigrateTip;
        set
        {
            if (value == _showComponentsMigrateTip) return;
            _showComponentsMigrateTip = value;
            OnPropertyChanged();
        }
    }

    public bool IsMigratedFromv1_4
    {
        get => _isMigratedFromv14;
        set
        {
            if (value == _isMigratedFromv14) return;
            _isMigratedFromv14 = value;
            OnPropertyChanged();
        }
    }

    public bool IsProfileEditorClassInfoSubjectAutoMoveNextEnabled
    {
        get => _isProfileEditorClassInfoSubjectAutoMoveNextEnabled;
        set
        {
            if (value == _isProfileEditorClassInfoSubjectAutoMoveNextEnabled) return;
            _isProfileEditorClassInfoSubjectAutoMoveNextEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsSwapMode
    {
        get => _isSwapMode;
        set
        {
            if (value == _isSwapMode) return;
            _isSwapMode = value;
            OnPropertyChanged();
        }
    }

    public bool ShowEchoCaveWhenSettingsPageLoading
    {
        get => _showEchoCaveWhenSettingsPageLoading;
        set
        {
            if (value == _showEchoCaveWhenSettingsPageLoading) return;
            _showEchoCaveWhenSettingsPageLoading = value;
            OnPropertyChanged();
        }
    }

    public int SettingsPagesCachePolicy
    {
        get => _settingsPagesCachePolicy;
        set
        {
            if (value == _settingsPagesCachePolicy) return;
            _settingsPagesCachePolicy = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Guid> TrustedProfileIds
    {
        get => _trustedProfileIds;
        set
        {
            if (Equals(value, _trustedProfileIds)) return;
            _trustedProfileIds = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public bool ShowSellingAnnouncement
    {
        get => GlobalStorageService.GetValue("ShowSellingAnnouncement") is "1" or null;
        set
        {
            try
            {
                var envVar = value ? "1" : "0";
                GlobalStorageService.SetValue("ShowSellingAnnouncement", envVar);
                OnPropertyChanged();
            }
            catch (Exception ex)
            {
                IAppHost.GetService<ILogger<Settings>>().LogError(ex, "无法设置 ShowSellingAnnouncement 启用状态。");
            }

        }
    }
}
