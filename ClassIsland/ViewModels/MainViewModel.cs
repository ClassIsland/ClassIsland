using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using ClassIsland.Controls.EditMode;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Components;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Models;
using ClassIsland.Services;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    private Profile _profile = new();
    private Settings _settings = new();
    private bool _isOverlayOpened = false;
    private bool _isMouseIn = false;
    private bool _isMainWindowFaded = false;
    private bool _isForegroundFullscreen = false;
    private bool _isForegroundMaxWindow = false;
    private string _currentProfilePath = "Profile.json";
    private double _gridRootLeft = 0;
    private double _gridRootTop = 0;
    private double _overlayRemainTimePercents = 1;
    private NotificationRequest _currentNotificationRequest = new();
    private bool _isMainWindowVisible = true;
    private bool _isClosing = false;
    private bool _isBusy = false;
    private DateTime _firstProcessNotifications = DateTime.MinValue;
    private DateTime _debugCurrentTime = DateTime.Now;
    private bool _isNotificationWindowExplicitShowed = false;
    private bool _isHideRuleSatisfied = false;
    private string? _lastStoryboardName;
    private NotificationContent? _currentMaskContent;
    private NotificationContent? _currentOverlayContent;
    private double _actualRootOffsetX = 0;
    private double _actualRootOffsetY = 0.0;
    private Rect _actualClientBound = new();
    private ObservableCollection<Control> _effectControls = [];
    private bool _isEditMode = false;
    private bool _editModeIsWindowMode = false;
    private EditModeView? _editModeView = null;


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

    public object? CurrentMaskElement => CurrentMaskContent?.Content;

    public object? CurrentOverlayElement => CurrentOverlayContent?.Content;

    public NotificationContent? CurrentMaskContent
    {
        get => _currentMaskContent;
        set
        {
            if (Equals(value, _currentMaskContent)) return;
            _currentMaskContent = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentMaskElement));
        }
    }

    public NotificationContent? CurrentOverlayContent
    {
        get => _currentOverlayContent;
        set
        {
            if (Equals(value, _currentOverlayContent)) return;
            _currentOverlayContent = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentOverlayElement));
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

    public bool IsMainWindowFaded
    {
        get => _isMainWindowFaded;
        set
        {
            if (value == _isMainWindowFaded) return;
            _isMainWindowFaded = value;
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
            //Settings.NotifyPropertyChanged(nameof(Settings.HideOnFullscreen));
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
            //Settings.NotifyPropertyChanged(nameof(Settings.HideOnMaxWindow));
        }
    }

    public bool IsHideRuleSatisfied
    {
        get => _isHideRuleSatisfied;
        set
        {
            if (value == _isHideRuleSatisfied) return;
            _isHideRuleSatisfied = value;
            OnPropertyChanged();
        }
    }

    public string CurrentProfilePath
    {
        get => App.GetService<IProfileService>().CurrentProfilePath;
        set
        {
            if (value == _currentProfilePath) return;
            App.GetService<IProfileService>().CurrentProfilePath = value;
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

    public string? LastStoryboardName
    {
        get => _lastStoryboardName;
        set
        {
            if (value == _lastStoryboardName) return;
            _lastStoryboardName = value;
            OnPropertyChanged();
        }
    }

    public double ActualRootOffsetX
    {
        get => _actualRootOffsetX;
        set
        {
            if (value.Equals(_actualRootOffsetX)) return;
            _actualRootOffsetX = value;
            OnPropertyChanged();
        }
    }

    public double ActualRootOffsetY
    {
        get => _actualRootOffsetY;
        set
        {
            if (value.Equals(_actualRootOffsetY)) return;
            _actualRootOffsetY = value;
            OnPropertyChanged();
        }
    }

    public Rect ActualClientBound
    {
        get => _actualClientBound;
        set
        {
            if (value.Equals(_actualClientBound)) return;
            _actualClientBound = value;
            OnPropertyChanged();
        }
    }
    
    public ObservableCollection<Control> EffectControls
    {
        get => _effectControls;
        set
        {
            if (Equals(value, _effectControls)) return;
            _effectControls = value;
            OnPropertyChanged();
        }
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            if (value == _isEditMode) return;
            _isEditMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsWindowMode));
        }
    }

    public bool EditModeIsWindowMode
    {
        get => _editModeIsWindowMode;
        set
        {
            if (value == _editModeIsWindowMode) return;
            _editModeIsWindowMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsWindowMode));
        }
    }

    public bool IsWindowMode => EditModeIsWindowMode && IsEditMode;

    public EditModeView? EditModeView
    {
        get => _editModeView;
        set
        {
            if (Equals(value, _editModeView)) return;
            _editModeView = value;
            OnPropertyChanged();
        }
    }
    
    [ObservableProperty] private MainWindowLineSettings? _selectedMainWindowLineSettings;
    [ObservableProperty] private Dictionary<MainWindowLineSettings, EditableComponentsListBox> _mainWindowLineListBoxCache = new();
    [ObservableProperty] private Dictionary<EditableComponentsListBox, MainWindowLineSettings> _mainWindowLineListBoxCacheReversed = new();
    [ObservableProperty] private ComponentSettings? _selectedComponentSettings;

}