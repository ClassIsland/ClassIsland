using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.ViewModels;

public class ProfileSettingsViewModel : ObservableRecipient
{
    private object _drawerContent = new();
    private bool _isClassPlansEditing = false;
    private SnackbarMessageQueue _messageQueue = new();

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
}