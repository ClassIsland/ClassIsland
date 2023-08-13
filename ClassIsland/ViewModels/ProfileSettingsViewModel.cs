using System;
using System.Collections.ObjectModel;
using ClassIsland.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.ViewModels;

public class ProfileSettingsViewModel : ObservableRecipient
{
    private object _drawerContent = new();
    private bool _isClassPlansEditing = false;
    private SnackbarMessageQueue _messageQueue = new();
    private ObservableCollection<string> _profiles = new();
    private bool _isRestartSnackbarActive = false;
    private string _renameProfileName = "";
    private string _createProfileName = "";
    private string _selectedProfile = "";
    private string _deleteConfirmField = "";
    private bool _isOfflineEditor = false;
    private TimeLayoutItem? _selectedTimePoint;

    public object DrawerContent
    {
        get => _drawerContent;
        set
        {
            if (Equals(value, _drawerContent)) return;
            _drawerContent = value;
            OnPropertyChanged();
        }
    }

    public bool IsClassPlansEditing
    {
        get => _isClassPlansEditing;
        set
        {
            if (value == _isClassPlansEditing) return;
            _isClassPlansEditing = value;
            OnPropertyChanged();
        }
    }

    public SnackbarMessageQueue MessageQueue
    {
        get => _messageQueue;
        set
        {
            if (Equals(value, _messageQueue)) return;
            _messageQueue = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> Profiles
    {
        get => _profiles;
        set
        {
            if (Equals(value, _profiles)) return;
            _profiles = value;
            OnPropertyChanged();
        }
    }

    public bool IsRestartSnackbarActive
    {
        get => _isRestartSnackbarActive;
        set
        {
            if (value == _isRestartSnackbarActive) return;
            _isRestartSnackbarActive = value;
            OnPropertyChanged();
        }
    }

    public string CreateProfileName
    {
        get => _createProfileName;
        set
        {
            if (value == _createProfileName) return;
            _createProfileName = value;
            OnPropertyChanged();
        }
    }

    public string RenameProfileName
    {
        get => _renameProfileName;
        set
        {
            if (value == _renameProfileName) return;
            _renameProfileName = value;
            OnPropertyChanged();
        }
    }

    public string SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (value == _selectedProfile) return;
            _selectedProfile = value;
            OnPropertyChanged();
        }
    }

    public string DeleteConfirmField
    {
        get => _deleteConfirmField;
        set
        {
            if (value == _deleteConfirmField) return;
            _deleteConfirmField = value;
            OnPropertyChanged();
        }
    }

    public bool IsOfflineEditor
    {
        get => _isOfflineEditor;
        set
        {
            if (value == _isOfflineEditor) return;
            _isOfflineEditor = value;
            OnPropertyChanged();
        }
    }

    public Guid DialogHostId
    {
        get;
    } = Guid.NewGuid();

    public TimeLayoutItem? SelectedTimePoint
    {
        get => _selectedTimePoint;
        set
        {
            if (Equals(value, _selectedTimePoint)) return;
            _selectedTimePoint = value;
            OnPropertyChanged();
        }
    }
}