using System;
using ClassIsland.Enums;
using ClassIsland.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class MainViewModel : ObservableRecipient
{
    private Profile _profile = new();
    private ClassPlan? _currentClassPlan = new();
    private int? _currentSelectedIndex = null;
    private Settings _settings = new();
    private object? _currentMaskElement;
    private object? _currentOverlayElement;
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
        }
    }
}