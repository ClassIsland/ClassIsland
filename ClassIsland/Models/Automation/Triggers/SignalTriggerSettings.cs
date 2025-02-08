using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.Automation.Triggers;

public class SignalTriggerSettings : ObservableRecipient
{
    private string _signalName = string.Empty;
    private bool _isRevert = false;

    public string SignalName
    {
        get => _signalName;
        set
        {
            if (value == _signalName) return;
            _signalName = value;
            OnPropertyChanged();
        }
    }

    public bool IsRevert
    {
        get => _isRevert;
        set
        {
            if (value == _isRevert) return;
            _isRevert = value;
            OnPropertyChanged();
        }
    }
}