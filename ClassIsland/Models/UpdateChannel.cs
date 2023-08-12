using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class UpdateChannel : ObservableRecipient
{
    private string _name = "";
    private string _description = "";
    private string _rootUrl = "";

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

    public string RootUrl
    {
        get => _rootUrl;
        set
        {
            if (value == _rootUrl) return;
            _rootUrl = value;
            OnPropertyChanged();
        }
    }
}