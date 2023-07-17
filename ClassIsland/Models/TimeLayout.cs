using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ClassIsland.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class TimeLayout : ObservableRecipient
{
    private ObservableCollection<TimeLayoutItem> _layouts = new();
    private string _name = "";

    public TimeLayout()
    {
        PropertyChanged += OnPropertyChanged;
        Layouts.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(Layouts));
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Debug.WriteLine(e.PropertyName);
        switch (e.PropertyName)
        {
            case nameof(Layouts):
                Layouts.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(Layouts));
                break;

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
}