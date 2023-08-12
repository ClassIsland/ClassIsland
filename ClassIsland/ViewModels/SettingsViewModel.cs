using System;
using System.Windows.Documents;
using System.Windows.Forms;
using ClassIsland.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class SettingsViewModel : ObservableRecipient
{
    private Screen[] _screens = Array.Empty<Screen>();
    private string _license = "";
    private object? _drawerContent = new();
    private FlowDocument _currentMarkdownDocument = new();
    private UpdateChannel _selectedChannelModel = new();
    private int _appIconClickCount = 0;

    public Screen[] Screens
    {
        get => _screens;
        set
        {
            if (Equals(value, _screens)) return;
            _screens = value;
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

    public FlowDocument CurrentMarkdownDocument
    {
        get => _currentMarkdownDocument;
        set
        {
            if (Equals(value, _currentMarkdownDocument)) return;
            _currentMarkdownDocument = value;
            OnPropertyChanged();
        }
    }

    public UpdateChannel SelectedChannelModel
    {
        get => _selectedChannelModel;
        set
        {
            if (Equals(value, _selectedChannelModel)) return;
            _selectedChannelModel = value;
            OnPropertyChanged();
        }
    }

    public int AppIconClickCount
    {
        get => _appIconClickCount;
        set
        {
            if (value == _appIconClickCount) return;
            _appIconClickCount = value;
            OnPropertyChanged();
        }
    }
}