using ClassIsland.Core.Enums;
using ClassIsland.Core.Models.ProfileAnalyzing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class AttachedSettingsNodeWithState : ObservableRecipient
{
    private AttachableObjectNode _node = new();
    private AttachableObjectAddress _address = new();
    private AttachedSettingsControlState _state = AttachedSettingsControlState.Disabled;

    public AttachableObjectNode Node
    {
        get => _node;
        set
        {
            if (Equals(value, _node)) return;
            _node = value;
            OnPropertyChanged();
        }
    }

    public AttachableObjectAddress Address
    {
        get => _address;
        set
        {
            if (Equals(value, _address)) return;
            _address = value;
            OnPropertyChanged();
        }
    }

    public AttachedSettingsControlState State
    {
        get => _state;
        set
        {
            if (value == _state) return;
            _state = value;
            OnPropertyChanged();
        }
    }
}