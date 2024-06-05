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
}