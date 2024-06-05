using ClassIsland.Core.Attributes;
using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.ViewModels;

public class SettingsNewViewModel : ObservableRecipient
{
    private object? _frameContent;
    private SettingsPageInfo? _selectedPageInfo = null;
    private bool _isViewCompressed = false;
    private bool _isNavigationDrawerOpened = true;
    private bool _canGoBack = false;
    private object? _drawerContent;
    private SnackbarMessageQueue _snackbarMessageQueue = new();
    private bool _isDrawerOpen = false;
    private bool _isRequestedRestart = false;

    public object? FrameContent
    {
        get => _frameContent;
        set
        {
            if (Equals(value, _frameContent)) return;
            _frameContent = value;
            OnPropertyChanged();
        }
    }

    public SettingsPageInfo? SelectedPageInfo
    {
        get => _selectedPageInfo;
        set
        {
            if (Equals(value, _selectedPageInfo)) return;
            _selectedPageInfo = value;
            OnPropertyChanged();
        }
    }

    public bool IsViewCompressed
    {
        get => _isViewCompressed;
        set
        {
            if (value == _isViewCompressed) return;
            _isViewCompressed = value;
            OnPropertyChanged();
        }
    }

    public bool IsNavigationDrawerOpened
    {
        get => _isNavigationDrawerOpened;
        set
        {
            if (value == _isNavigationDrawerOpened) return;
            _isNavigationDrawerOpened = value;
            OnPropertyChanged();
        }
    }

    public bool CanGoBack
    {
        get => _canGoBack;
        set
        {
            if (value == _canGoBack) return;
            _canGoBack = value;
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

    public SnackbarMessageQueue SnackbarMessageQueue
    {
        get => _snackbarMessageQueue;
        set
        {
            if (Equals(value, _snackbarMessageQueue)) return;
            _snackbarMessageQueue = value;
            OnPropertyChanged();
        }
    }

    public bool IsDrawerOpen
    {
        get => _isDrawerOpen;
        set
        {
            if (value == _isDrawerOpen) return;
            _isDrawerOpen = value;
            OnPropertyChanged();
        }
    }

    public bool IsRequestedRestart
    {
        get => _isRequestedRestart;
        set
        {
            if (value == _isRequestedRestart) return;
            _isRequestedRestart = value;
            OnPropertyChanged();
        }
    }
}