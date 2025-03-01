using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.RecoveryPages;

public partial class RecoverBackupViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<string> _backups = [];

    [ObservableProperty] private int _recoverMode = 1;

    [ObservableProperty] private string? _selectedBackupName;

    [ObservableProperty] private bool _isWorking;
}