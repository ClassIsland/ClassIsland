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
    private int _flipNextCount = 0;
    private bool _isFlipEnd = false;
    private int _flipIndex = 0;
    private bool _createStartupShortcut = true;
    private bool _createStartMenuShortcut = true;
    private bool _createDesktopShortcut = false;
    private int _slideIndexMaster = 0;
    private bool _registerUrlScheme = false;
    private bool _createClassSwapShortcut = false;

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

    public int SlideIndexMaster
    {
        get => _slideIndexMaster;
        set
        {
            if (value == _slideIndexMaster) return;
            _slideIndexMaster = value;
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

    public int FlipNextCount
    {
        get => _flipNextCount;
        set
        {
            if (value == _flipNextCount) return;
            _flipNextCount = value;
            OnPropertyChanged();
        }
    }

    public bool IsFlipEnd
    {
        get => _isFlipEnd;
        set
        {
            if (value == _isFlipEnd) return;
            _isFlipEnd = value;
            OnPropertyChanged();
        }
    }

    public int FlipIndex
    {
        get => _flipIndex;
        set
        {
            if (value == _flipIndex) return;
            _flipIndex = value;
            OnPropertyChanged();
        }
    }

    public bool CreateStartupShortcut
    {
        get => _createStartupShortcut;
        set
        {
            if (value == _createStartupShortcut) return;
            _createStartupShortcut = value;
            OnPropertyChanged();
        }
    }

    public bool CreateStartMenuShortcut
    {
        get => _createStartMenuShortcut;
        set
        {
            if (value == _createStartMenuShortcut) return;
            _createStartMenuShortcut = value;
            OnPropertyChanged();
        }
    }

    public bool CreateDesktopShortcut
    {
        get => _createDesktopShortcut;
        set
        {
            if (value == _createDesktopShortcut) return;
            _createDesktopShortcut = value;
            OnPropertyChanged();
        }
    }

    public bool RegisterUrlScheme
    {
        get => _registerUrlScheme;
        set
        {
            if (value == _registerUrlScheme) return;
            _registerUrlScheme = value;
            OnPropertyChanged();
        }
    }

    public bool CreateClassSwapShortcut
    {
        get => _createClassSwapShortcut;
        set
        {
            if (value == _createClassSwapShortcut) return;
            _createClassSwapShortcut = value;
            OnPropertyChanged();
        }
    }
}