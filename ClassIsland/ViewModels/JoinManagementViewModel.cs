using ClassIsland.Core.Models.Management;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class JoinManagementViewModel : ObservableRecipient
{
    private string _configFilePath = "";
    private ManagementSettings _managementSettings = new();
    private string _errorMessage = "";
    private bool _isConfigLoaded = false;
    private bool _isErrorMessageOpen = false;
    private bool _isWorking = false;

    public string ConfigFilePath
    {
        get => _configFilePath;
        set
        {
            if (value == _configFilePath) return;
            _configFilePath = value;
            OnPropertyChanged();
        }
    }

    public ManagementSettings ManagementSettings
    {
        get => _managementSettings;
        set
        {
            if (Equals(value, _managementSettings)) return;
            _managementSettings = value;
            OnPropertyChanged();
        }
    }

    public bool IsConfigLoaded
    {
        get => _isConfigLoaded;
        set
        {
            if (value == _isConfigLoaded) return;
            _isConfigLoaded = value;
            OnPropertyChanged();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (value == _errorMessage) return;
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsErrorMessageOpen
    {
        get => _isErrorMessageOpen;
        set
        {
            if (value == _isErrorMessageOpen) return;
            _isErrorMessageOpen = value;
            OnPropertyChanged();
        }
    }

    public bool IsWorking
    {
        get => _isWorking;
        set
        {
            if (value == _isWorking) return;
            _isWorking = value;
            OnPropertyChanged();
        }
    }
}