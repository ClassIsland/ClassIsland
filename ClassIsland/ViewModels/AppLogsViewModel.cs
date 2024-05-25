using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class AppLogsViewModel : ObservableRecipient
{
    private string _filterText = "";
    private ObservableCollection<int> _filterTypes = new();
    private bool _isFilteredCritical = true;
    private bool _isFilteredError = true;
    private bool _isFilteredWarning = true;
    private bool _isFilteredInfo = false;
    private bool _isFilteredDebug = false;
    private bool _isFilteredTrace = false;

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (value == _filterText) return;
            _filterText = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<int> FilterTypes
    {
        get => _filterTypes;
        set
        {
            if (Equals(value, _filterTypes)) return;
            _filterTypes = value;
            OnPropertyChanged();
        }
    }

    public bool IsFilteredCritical
    {
        get => _isFilteredCritical;
        set
        {
            if (value == _isFilteredCritical) return;
            _isFilteredCritical = value;
            OnPropertyChanged();
        }
    }

    public bool IsFilteredError
    {
        get => _isFilteredError;
        set
        {
            if (value == _isFilteredError) return;
            _isFilteredError = value;
            OnPropertyChanged();
        }
    }

    public bool IsFilteredWarning
    {
        get => _isFilteredWarning;
        set
        {
            if (value == _isFilteredWarning) return;
            _isFilteredWarning = value;
            OnPropertyChanged();
        }
    }

    public bool IsFilteredInfo
    {
        get => _isFilteredInfo;
        set
        {
            if (value == _isFilteredInfo) return;
            _isFilteredInfo = value;
            OnPropertyChanged();
        }
    }

    public bool IsFilteredDebug
    {
        get => _isFilteredDebug;
        set
        {
            if (value == _isFilteredDebug) return;
            _isFilteredDebug = value;
            OnPropertyChanged();
        }
    }

    public bool IsFilteredTrace
    {
        get => _isFilteredTrace;
        set
        {
            if (value == _isFilteredTrace) return;
            _isFilteredTrace = value;
            OnPropertyChanged();
        }
    }
}