using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using ClassIsland.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class TimeLayout : AttachableSettingsObject
{
    private ObservableCollection<TimeLayoutItem> _layouts = new();
    private string _name = "新时间表";
    private bool _isActivated = false;
    private bool _isActivatedManually = false;

    public TimeLayout()
    {
        PropertyChanged += OnPropertyChanged;
        Layouts.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(Layouts));
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        //Debug.WriteLine(e.PropertyName);
        //switch (e.PropertyName)
        //{
        //    case nameof(Layouts):
        //        Layouts.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(Layouts));
        //        break;

        //}
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

    public ObservableCollection<TimeLayoutItem> Layouts
    {
        get => _layouts;
        set
        {
            if (Equals(value, _layouts)) return;
            _layouts = value;
            OnPropertyChanged();
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

    [JsonIgnore]
    public bool IsActivatedManually
    {
        get => _isActivatedManually;
        set
        {
            if (value == _isActivatedManually) return;
            _isActivatedManually = value;
            OnPropertyChanged();
        }
    }
}