using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using ClassIsland.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Color = System.Windows.Media.Color;

namespace ClassIsland.ViewModels;

public class SettingsViewModel : ObservableRecipient
{
    private Screen[] _screens = Array.Empty<Screen>();
    private string _license = "";
    private object? _drawerContent = new();
    private FlowDocument _currentMarkdownDocument = new();
    private UpdateChannel _selectedChannelModel = new();
    private int _appIconClickCount = 0;
    private BitmapImage _testImage = new();
    private string _debugOutputs = "";
    private ObservableCollection<Color> _debugImageAccentColors = new();

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

    public BitmapImage TestImage
    {
        get => _testImage;
        set
        {
            if (Equals(value, _testImage)) return;
            _testImage = value;
            OnPropertyChanged();
        }
    }

    public string DebugOutputs
    {
        get => _debugOutputs;
        set
        {
            if (value == _debugOutputs) return;
            _debugOutputs = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Color> DebugImageAccentColors
    {
        get => _debugImageAccentColors;
        set
        {
            if (Equals(value, _debugImageAccentColors)) return;
            _debugImageAccentColors = value;
            OnPropertyChanged();
        }
    }
}