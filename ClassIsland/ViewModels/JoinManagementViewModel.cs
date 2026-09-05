using ClassIsland.Shared.Models.Management;

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

    private bool _isBashuMode = true;
    private string _bashuPairingCode = "";
    private string _bashuServerUrl = "https://bashu.cqaibase.cn";
    private string _bashuDeviceName = "班级多媒体大屏";

    public bool IsBashuMode
    {
        get => _isBashuMode;
        set
        {
            if (value == _isBashuMode) return;
            _isBashuMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanConnect));
        }
    }

    public string BashuPairingCode
    {
        get => _bashuPairingCode;
        set
        {
            if (value == _bashuPairingCode) return;
            _bashuPairingCode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanConnect));
        }
    }

    public string BashuServerUrl
    {
        get => _bashuServerUrl;
        set
        {
            if (value == _bashuServerUrl) return;
            _bashuServerUrl = value;
            OnPropertyChanged();
        }
    }

    public string BashuDeviceName
    {
        get => _bashuDeviceName;
        set
        {
            if (value == _bashuDeviceName) return;
            _bashuDeviceName = value;
            OnPropertyChanged();
        }
    }

    public bool CanConnect => IsBashuMode ? !string.IsNullOrWhiteSpace(BashuPairingCode) : IsConfigLoaded;
}