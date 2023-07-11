using ClassIsland.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class MainViewModel : ObservableRecipient
{
    private Profile _profile = new();

    public Profile Profile
    {
        get => _profile;
        set
        {
            if (Equals(value, _profile)) return;
            _profile = value;
            OnPropertyChanged();
        }
    }
}