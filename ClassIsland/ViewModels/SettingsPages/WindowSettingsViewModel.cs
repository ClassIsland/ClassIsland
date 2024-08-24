using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public class WindowSettingsViewModel : ObservableRecipient
{
    private ObservableCollection<Screen> _screens = new();

    public ObservableCollection<Screen> Screens
    {
        get => _screens;
        set
        {
            if (Equals(value, _screens)) return;
            _screens = value;
            OnPropertyChanged();
        }
    }
}