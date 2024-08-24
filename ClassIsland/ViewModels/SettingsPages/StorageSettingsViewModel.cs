using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public class StorageSettingsViewModel : ObservableRecipient
{
    private bool _isBackingUp = false;
    private bool _isBackupFinished = false;

    public bool IsBackingUp
    {
        get => _isBackingUp;
        set
        {
            if (value == _isBackingUp) return;
            _isBackingUp = value;
            OnPropertyChanged();
        }
    }

    public bool IsBackupFinished
    {
        get => _isBackupFinished;
        set
        {
            if (value == _isBackupFinished) return;
            _isBackupFinished = value;
            OnPropertyChanged();
        }
    }
}