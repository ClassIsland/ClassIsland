using System;
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
}