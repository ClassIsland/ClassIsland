using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class Profile : ObservableRecipient
{
    private string _name = "";
    private Dictionary<string, TimeLayout> _timeLayouts = new();
    private Dictionary<string, ClassPlan> _classPlans = new();
    private ObservableCollection<string> _subjects = new();

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

    public Dictionary<string, TimeLayout> TimeLayouts
    {
        get => _timeLayouts;
        set
        {
            if (Equals(value, _timeLayouts)) return;
            _timeLayouts = value;
            OnPropertyChanged();
        }
    }

    public Dictionary<string, ClassPlan> ClassPlans
    {
        get => _classPlans;
        set
        {
            if (Equals(value, _classPlans)) return;
            _classPlans = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> Subjects
    {
        get => _subjects;
        set
        {
            if (Equals(value, _subjects)) return;
            _subjects = value;
            OnPropertyChanged();
        }
    }
}