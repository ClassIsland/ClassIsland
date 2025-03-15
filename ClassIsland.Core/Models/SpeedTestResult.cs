using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models;

public class SpeedTestResult : ObservableRecipient
{
    private long _delay = 0;
    private bool _canConnect = false;
    private bool _isTested = false;
    private bool _isTesting = false;
    private bool _isDelayUnclear = false;

    public long Delay
    {
        get => _delay;
        set
        {
            if (value == _delay) return;
            _delay = value;
            OnPropertyChanged();
        }
    }

    public bool CanConnect
    {
        get => _canConnect;
        set
        {
            if (value == _canConnect) return;
            _canConnect = value;
            OnPropertyChanged();
        }
    }

    public bool IsTested
    {
        get => _isTested;
        set
        {
            if (value == _isTested) return;
            _isTested = value;
            OnPropertyChanged();
        }
    }

    public bool IsTesting
    {
        get => _isTesting;
        set
        {
            if (value == _isTesting) return;
            _isTesting = value;
            OnPropertyChanged();
        }
    }

    public bool IsDelayUnclear
    {
        get => _isDelayUnclear;
        set
        {
            if (value == _isDelayUnclear) return;
            _isDelayUnclear = value;
            OnPropertyChanged();
        }
    }
}