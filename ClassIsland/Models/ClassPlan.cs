using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices;
using System.Linq;
using System.Text.Json.Serialization;
using ClassIsland.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class ClassPlan : AttachableSettingsObject
{
    private string _timeLayoutId = "";
    private ObservableCollection<ClassInfo> _classes = new();
    private string _name = "新课表";
    private ObservableDictionary<string, TimeLayout> _timeLayouts = new();
    private TimeRule _timeRule = new();
    private bool _isActivated = false;

    public ClassPlan()
    {
        PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RefreshClassesList();
    }

    [JsonIgnore]
    public ObservableDictionary<string, TimeLayout> TimeLayouts
    {
        get => _timeLayouts;
        set
        {
            if (Equals(value, _timeLayouts)) return;
            _timeLayouts = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TimeLayout));
        }
    }

    [JsonIgnore] public TimeLayout TimeLayout => TimeLayouts[TimeLayoutId];

    public string TimeLayoutId
    {
        get => _timeLayoutId;
        set
        {
            if (value == _timeLayoutId) return;
            _timeLayoutId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TimeLayout));
        }
    }

    public TimeRule TimeRule
    {
        get => _timeRule;
        set
        {
            if (Equals(value, _timeRule)) return;
            _timeRule = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<ClassInfo> Classes
    {
        get => _classes;
        set
        {
            if (Equals(value, _classes)) return;
            _classes = value;
            OnPropertyChanged();
        }
    }

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

    public void RefreshClassesList()
    {
        // 对齐长度
        if (TimeLayoutId == null || !TimeLayouts.ContainsKey(TimeLayoutId))
        {
            return;
        }
        
        var l = (from i in TimeLayout.Layouts where i.TimeType == 0 select i).Count();
        //Debug.WriteLine(l);
        if (Classes.Count < l)
        {
            var d = l - Classes.Count;
            for (var i = 0; i < d; i++)
            {
                Classes.Add(new ClassInfo());
            }
        }
        else if (Classes.Count > l) 
        {
            var d = Classes.Count - l;
            for (var i = 0; i < d; i++)
            {
                Classes.RemoveAt(Classes.Count - 1);
            }
        }

        for (var i = 0; i < Classes.Count; i++)
        {
            Classes[i].Index = i;
            Classes[i].CurrentTimeLayout = TimeLayout;
        }
    }

    [JsonIgnore]
    public bool IsActivated
    {
        get => _isActivated;
        set
        {
            if (value == _isActivated) return;
            _isActivated = value;
            OnPropertyChanged();
        }
    }
}