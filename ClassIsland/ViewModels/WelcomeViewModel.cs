using System;
using ClassIsland.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class WelcomeViewModel : ObservableRecipient
{
    private int _slideIndex = 0;
    private bool _isLicenseAgreed = false;
    private Settings _settings = new();
    private string _license = "";
    private bool _isExitConfirmed = false;
    private int _masterTabIndex = 0;

    public Guid DialogId
    {
        get;
    } = Guid.NewGuid();

    public int MasterTabIndex
    {
        get => _masterTabIndex;
        set
        {
            if (value == _masterTabIndex) return;
            _masterTabIndex = value;
            OnPropertyChanged();
        }
    }

    public int SlideIndex
    {
        get => _slideIndex;
        set
        {
            if (value == _slideIndex) return;
            _slideIndex = value;
            OnPropertyChanged();
        }
    }

    public bool IsLicenseAgreed
    {
        get => _isLicenseAgreed;
        set
        {
            if (value == _isLicenseAgreed) return;
            _isLicenseAgreed = value;
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

    public bool IsExitConfirmed
    {
        get => _isExitConfirmed;
        set
        {
            if (value == _isExitConfirmed) return;
            _isExitConfirmed = value;
            OnPropertyChanged();
        }
    }
}