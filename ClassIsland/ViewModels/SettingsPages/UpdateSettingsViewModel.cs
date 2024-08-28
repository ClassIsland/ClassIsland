﻿using System.Windows.Documents;
using ClassIsland.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public class UpdateSettingsViewModel : ObservableRecipient
{
    private FlowDocument _currentMarkdownDocument = new();
    private UpdateChannel _selectedChannelModel = new();
    private FlowDocument _changeLogs = new();

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

    public FlowDocument ChangeLogs
    {
        get => _changeLogs;
        set
        {
            if (Equals(value, _changeLogs)) return;
            _changeLogs = value;
            OnPropertyChanged();
        }
    }
}