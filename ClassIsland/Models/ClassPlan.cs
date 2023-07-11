using System.Collections.ObjectModel;
using System.DirectoryServices;
using System.Linq;
using System.Text.Json.Serialization;
using ClassIsland.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class ClassPlan : ObservableRecipient
{
    private TimeLayout _timeLayout = new();
    private string _timeLayoutId = "";
    private ObservableCollection<string> _classes = new();
    private ObservableCollection<ITimeRule> _timeRules = new();

    [JsonIgnore]
    public TimeLayout TimeLayout
    {
        get => _timeLayout;
        set
        {
            if (Equals(value, _timeLayout)) return;
            _timeLayout = value;
            OnPropertyChanged();
        }
    }

    public string TimeLayoutId
    {
        get => _timeLayoutId;
        set
        {
            if (value == _timeLayoutId) return;
            _timeLayoutId = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<ITimeRule> TimeRules
    {
        get => _timeRules;
        set
        {
            if (Equals(value, _timeRules)) return;
            _timeRules = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsAllRuleSatisfied));
        }
    }

    public bool IsAllRuleSatisfied => TimeRules.All(i => i.IsSatisfied);

    public ObservableCollection<string> Classes
    {
        get => _classes;
        set
        {
            if (Equals(value, _classes)) return;
            _classes = value;
            OnPropertyChanged();
        }
    }
}