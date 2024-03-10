using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ClassIsland.Core.Models.Profile;

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

    public event EventHandler? LayoutObjectChanged;

    public event EventHandler<TimeLayoutUpdateEventArgs>? LayoutItemChanged;

    public void SortCompleted()
    {
        LayoutObjectChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        //Debug.WriteLine(e.PropertyName);
        switch (e.PropertyName)
        {
            case nameof(Layouts):
                //LayoutObjectChanged?.Invoke(this, EventArgs.Empty);
                break;

        }
    }

    public void InsertTimePoint(int index, TimeLayoutItem item)
    {
        Layouts.Insert(index, item);
        var ci = -1;
        if (item.TimeType == 0)
        {
            ci = (from i in Layouts where i.TimeType == 0 select i).ToList().IndexOf(item);
        }
        LayoutItemChanged?.Invoke(this, new TimeLayoutUpdateEventArgs()
        {
            Action = NotifyCollectionChangedAction.Add,
            AddedItems = { item },
            AddIndex = index,
            AddIndexClasses = ci
        });
    }

    public void RemoveTimePoint(TimeLayoutItem item)
    {
        var index = Layouts.IndexOf(item);
        var ci = -1;
        if (item.TimeType == 0)
        {
            ci = (from i in Layouts where i.TimeType==0 select i).ToList().IndexOf(item);
        }
        Layouts.Remove(item);
        LayoutItemChanged?.Invoke(this, new TimeLayoutUpdateEventArgs()
        {
            Action = NotifyCollectionChangedAction.Remove,
            RemovedItems = { item },
            RemoveIndex = index,
            RemoveIndexClasses = ci
        });
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