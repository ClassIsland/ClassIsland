using System;
using System.Windows.Media;
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
    private bool _hideOnClass = true;
    private bool _isClassChangingNotificationEnabled = true;
    private bool _isClassPrepareNotificationEnabled = true;

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

    #endregion
}