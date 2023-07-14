using ClassIsland.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class MainViewModel : ObservableRecipient
{
    private Profile _profile = new();
    private ClassPlan? _currentClassPlan = new();
    private int _currentSelectedIndex = 0;

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

    public ClassPlan? CurrentClassPlan
    {
        get => _currentClassPlan;
        set
        {
            if (Equals(value, _currentClassPlan)) return;
            _currentClassPlan = value;
            OnPropertyChanged();
        }
    }

    public int CurrentSelectedIndex
    {
        get => _currentSelectedIndex;
        set
        {
            if (value == _currentSelectedIndex) return;
            _currentSelectedIndex = value;
            OnPropertyChanged();
        }
    }
}