using System;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignColors;

namespace ClassIsland.Models;

public class Settings : ObservableRecipient
{
    private int _theme = 0;
    private Color _primaryColor = Colors.DeepSkyBlue;
    private Color _secondaryColor = Colors.Aquamarine;
    private DateTime _singleWeekStartTime = DateTime.Now;
    private int _classPrepareNotifySeconds = 60;
    private bool _showDate = true;
    private bool _hideOnClass = false;
    private bool _isClassChangingNotificationEnabled = true;
    private bool _isClassPrepareNotificationEnabled = true;
    private int _windowDockingLocation = 1;
    private int _windowDockingOffsetX = 0;
    private int _windowDockingOffsetY = 0;
    private int _windowDockingMonitorIndex = 0;
    private int _windowLayer = 0;
    private bool _isMouseClickingEnabled = true;
    private bool _hideOnFullscreen = true;
    private bool _isClassOffNotificationEnabled = true;
    private ObservableCollection<string> _excludedFullscreenWindow = new()
    {
        "explorer"
    };

    private bool _hideOnMaxWindow = true;
    private double _opacity = 0.5;
    private bool _isDebugEnabled = false;
    private string _selectedProfile = "Default.json";

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

    #endregion

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
}