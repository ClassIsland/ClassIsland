using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Updating;

public class ChannelInfo : ObservableRecipient
{
    private string _name = "";
    private string _description = "";
    private string? _warnings = "";

    public string Name
    {
        get => _name;
        set
        {
            if (value == _name) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (value == _description) return;
            _description = value;
            OnPropertyChanged();
        }
    }

    public string? Warnings
    {
        get => _warnings;
        set
        {
            if (value == _warnings) return;
            _warnings = value;
            OnPropertyChanged();
        }
    }
}