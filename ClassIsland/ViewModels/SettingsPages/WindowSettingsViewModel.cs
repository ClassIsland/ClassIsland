using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public class WindowSettingsViewModel : ObservableRecipient
{
    private Screen[] _screens = Array.Empty<Screen>();

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
}