using System;
using System.Diagnostics;

using ClassIsland.Core.Enums;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Core.Models.Profile;
using ClassIsland.Models;
using ClassIsland.Services;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class MainViewModel : ObservableRecipient
{
    private Profile _profile = new();
    private ClassPlan? _currentClassPlan = new();
    private int? _currentSelectedIndex = null;
    private Settings _settings = new();
    private object? _currentMaskElement;
    private object? _currentOverlayElement = new object();
    private bool _isOverlayOpened = false;
    private Subject _nextSubject = new();
    private TimeLayoutItem _nextTimeLayoutItem = new();
    private TimeSpan _onClassLeftTime = TimeSpan.Zero;
    private DateTime _today = DateTime.Now;
    private bool _isMouseIn = false;
    private TimeState _currentStatus = TimeState.None;
    private TimeState _currentOverlayStatus = TimeState.None;
    private TimeLayoutItem _currentTimeLayoutItem = new();
    private bool _isForegroundFullscreen = false;
    private bool _isForegroundMaxWindow = false;
    private string _currentProfilePath = "Profile.json";
    private double _gridRootLeft = 0;
    private double _gridRootTop = 0;
    private TimeState _currentOverlayEventStatus = TimeState.None;
    private TimeLayoutItem _nextBreakingLayoutItem = new();
    private double _overlayRemainTimePercents = 1;
    private NotificationRequest _currentNotificationRequest = new();
    private bool _isMainWindowVisible = true;
    private Subject? _currentSubject = new();
    private bool _isClosing = false;
    private bool _isClassPlanEnabled = true;
    private bool _isBusy = false;
    private DateTime _firstProcessNotifications = DateTime.MinValue;
    private DateTime _debugCurrentTime = DateTime.Now;
    private bool _isNotificationWindowExplicitShowed = false;

    public Profile Profile
    {
        get => _profile;
        set
        {
            if (Equals(value, _profile)) return;
            _profile = value;
            OnPropertyChanged();
        }
    }

    public ClassPlan? CurrentClassPlan
    {
        get => _currentClassPlan;
        set
        {
            if (Equals(value, _currentClassPlan)) return;
            _currentClassPlan = value;
            OnPropertyChanged();
        }
    }

    public int? CurrentSelectedIndex
    {
        get => _currentSelectedIndex;
        set
        {
            if (value == _currentSelectedIndex) return;
            _currentSelectedIndex = value;
            OnPropertyChanged();
        }
    }

    public Settings Settings
    {
        get => _settings;
        set
        {
            if (Equals(value, _settings)) return;
            _settings = value;
            OnPropertyChanged();
        }
    }

    public object? CurrentMaskElement
    {
        get => _currentMaskElement;
        set
        {
            if (Equals(value, _currentMaskElement)) return;
            _currentMaskElement = value;
            OnPropertyChanged();
        }
    }

    public object? CurrentOverlayElement
    {
        get => _currentOverlayElement;
        set
        {
            if (Equals(value, _currentOverlayElement)) return;
            _currentOverlayElement = value;
            OnPropertyChanged();
        }
    }

    public bool IsOverlayOpened
    {
        get => _isOverlayOpened;
        set
        {
            if (value == _isOverlayOpened) return;
            _isOverlayOpened = value;
            OnPropertyChanged();
        }
    }

    public Subject NextSubject
    {
        get => _nextSubject;
        set
        {
            if (Equals(value, _nextSubject)) return;
            _nextSubject = value;
            OnPropertyChanged();
        }
    }

    public TimeLayoutItem NextTimeLayoutItem
    {
        get => _nextTimeLayoutItem;
        set
        {
            if (Equals(value, _nextTimeLayoutItem)) return;
            _nextTimeLayoutItem = value;
            OnPropertyChanged();
        }
    }

    public TimeLayoutItem NextBreakingLayoutItem
    {
        get => _nextBreakingLayoutItem;
        set
        {
            if (Equals(value, _nextBreakingLayoutItem)) return;
            _nextBreakingLayoutItem = value;
            OnPropertyChanged();
        }
    }

    public TimeSpan OnClassLeftTime
    {
        get => _onClassLeftTime;
        set
        {
            if (value.Equals(_onClassLeftTime)) return;
            _onClassLeftTime = value;
            OnPropertyChanged();
        }
    }

    public DateTime Today
    {
        get => _today;
        set
        {
            if (value.Equals(_today)) return;
            _today = value;
            OnPropertyChanged();
        }
    }

    public bool IsMouseIn
    {
        get => _isMouseIn;
        set
        {
            if (value == _isMouseIn) return;
            _isMouseIn = value;
            OnPropertyChanged();
        }
    }

    public TimeState CurrentStatus
    {
        get => _currentStatus;
        set
        {
            if (value == _currentStatus) return;
            _currentStatus = value;
            OnPropertyChanged();
        }
    }

    public TimeState CurrentOverlayStatus
    {
        get => _currentOverlayStatus;
        set
        {
            if (value == _currentOverlayStatus) return;
            _currentOverlayStatus = value;
            OnPropertyChanged();
        }
    }

    public TimeLayoutItem CurrentTimeLayoutItem
    {
        get => _currentTimeLayoutItem;
        set
        {
            if (Equals(value, _currentTimeLayoutItem)) return;
            _currentTimeLayoutItem = value;
            OnPropertyChanged();
        }
    }

    public bool IsForegroundFullscreen
    {
        get => _isForegroundFullscreen;
        set
        {
            if (value == _isForegroundFullscreen) return;
            _isForegroundFullscreen = value;
            OnPropertyChanged();
            Settings.NotifyPropertyChanged(nameof(Settings.HideOnFullscreen));
        }
    }

    public bool IsForegroundMaxWindow
    {
        get => _isForegroundMaxWindow;
        set
        {
            if (value == _isForegroundMaxWindow) return;
            _isForegroundMaxWindow = value;
            OnPropertyChanged();
            Settings.NotifyPropertyChanged(nameof(Settings.HideOnMaxWindow));
        }
    }

    public string CurrentProfilePath
    {
        get => App.GetService<ProfileService>().CurrentProfilePath;
        set
        {
            if (value == _currentProfilePath) return;
            App.GetService<ProfileService>().CurrentProfilePath = value;
            OnPropertyChanged();
        }
    }

    public double GridRootLeft
    {
        get => _gridRootLeft;
        set
        {
            if (value.Equals(_gridRootLeft)) return;
            _gridRootLeft = value;
            OnPropertyChanged();
        }
    }

    public double GridRootTop
    {
        get => _gridRootTop;
        set
        {
            if (value.Equals(_gridRootTop)) return;
            _gridRootTop = value;
            OnPropertyChanged();
        }
    }

    public SettingsService SettingsService => App.GetService<SettingsService>();

    public TimeState CurrentOverlayEventStatus
    {
        get => _currentOverlayEventStatus;
        set
        {
            if (value == _currentOverlayEventStatus) return;
            _currentOverlayEventStatus = value;
            OnPropertyChanged();
        }
    }

    public double OverlayRemainTimePercents
    {
        get => _overlayRemainTimePercents;
        set
        {
            if (value.Equals(_overlayRemainTimePercents)) return;
            _overlayRemainTimePercents = value;
            OnPropertyChanged();
        }
    }

    public Stopwatch OverlayRemainStopwatch { get; } = new();

    public NotificationRequest CurrentNotificationRequest
    {
        get => _currentNotificationRequest;
        set
        {
            if (Equals(value, _currentNotificationRequest)) return;
            _currentNotificationRequest = value;
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

    public Subject? CurrentSubject
    {
        get => _currentSubject;
        set
        {
            if (Equals(value, _currentSubject)) return;
            _currentSubject = value;
            OnPropertyChanged();
        }
    }

    public bool IsClosing
    {
        get => _isClosing;
        set
        {
            if (value == _isClosing) return;
            _isClosing = value;
            OnPropertyChanged();
        }
    }

    public bool IsClassPlanEnabled
    {
        get => _isClassPlanEnabled;
        set
        {
            if (value == _isClassPlanEnabled) return;
            _isClassPlanEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (value == _isBusy) return;
            _isBusy = value;
            OnPropertyChanged();
        }
    }

    public DateTime FirstProcessNotifications
    {
        get => _firstProcessNotifications;
        set
        {
            if (value.Equals(_firstProcessNotifications)) return;
            _firstProcessNotifications = value;
            OnPropertyChanged();
        }
    }

    public DateTime DebugCurrentTime
    {
        get => _debugCurrentTime;
        set
        {
            if (value.Equals(_debugCurrentTime)) return;
            _debugCurrentTime = value;
            OnPropertyChanged();
        }
    }

    public bool IsNotificationWindowExplicitShowed
    {
        get => _isNotificationWindowExplicitShowed;
        set
        {
            if (value == _isNotificationWindowExplicitShowed) return;
            _isNotificationWindowExplicitShowed = value;
            OnPropertyChanged();
        }
    }
}