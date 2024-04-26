using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ClassIsland.Core.Models.Profile;

public class ClassPlan : AttachableSettingsObject
{
    private string _timeLayoutId = "";
    private ObservableCollection<ClassInfo> _classes = new();
    private string _name = "新课表";
    private ObservableDictionary<string, TimeLayout> _timeLayouts = new();
    private TimeRule _timeRule = new();
    private bool _isActivated = false;
    private bool _isOverlay = false;
    private string? _overlaySourceId;
    private ClassPlan? _overlaySource = null;
    private bool _isEnabled = true;
    private DateTime _overlaySetupTime = DateTime.Now;
    private int _lastTimeLayoutCount = -1;

    public event EventHandler? ClassesChanged;

    public void NotifyClassesChanged()
    {
        ClassesChanged?.Invoke(this, EventArgs.Empty);
    }

    [JsonIgnore]
    public int LastTimeLayoutCount
    {
        get => _lastTimeLayoutCount;
        set
        {
            if (value == _lastTimeLayoutCount) return;
            _lastTimeLayoutCount = value;
            OnPropertyChanged();
        }
    }

    public ClassPlan()
    {
        PropertyChanged += OnPropertyChanged;
        if (TimeLayouts.ContainsKey(TimeLayoutId))
        {
            TimeLayout.LayoutObjectChanged += TimeLayoutOnLayoutObjectChanged;
            TimeLayout.Layouts.CollectionChanged += LayoutsOnCollectionChanged;
            TimeLayout.LayoutItemChanged += TimeLayoutOnLayoutItemChanged;
        }
        Classes.CollectionChanged += ClassesOnCollectionChanged;
    }

    private void ClassesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems == null)
                    return;
                foreach (var i in e.NewItems)
                {
                    if (i is ClassInfo c)
                    {
                        c.PropertyChanged += ClassInfoOnPropertyChanged;
                    }
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems == null)
                    return;
                foreach (var i in e.OldItems)
                {
                    if (i is ClassInfo c)
                    {
                        c.PropertyChanged -= ClassInfoOnPropertyChanged;
                    }
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                break;
            case NotifyCollectionChangedAction.Move:
                break;
            case NotifyCollectionChangedAction.Reset:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ClassInfoOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        NotifyClassesChanged();
    }

    private void TimeLayoutOnLayoutObjectChanged(object? sender, EventArgs e)
    {
        TimeLayout.Layouts.CollectionChanged += LayoutsOnCollectionChanged;
        RefreshClassesList(true);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(TimeLayout):
            case nameof(TimeLayoutId):
            {
                if (TimeLayouts.ContainsKey(TimeLayoutId))
                {
                    RefreshClassesList();
                    TimeLayout.LayoutObjectChanged += TimeLayoutOnLayoutObjectChanged;
                    TimeLayout.LayoutItemChanged += TimeLayoutOnLayoutItemChanged;
                    TimeLayout.Layouts.CollectionChanged += LayoutsOnCollectionChanged;
                }

                break;
            }
            case nameof(Classes):
            {
                Classes.CollectionChanged += ClassesOnCollectionChanged;
                foreach (var i in Classes)
                {
                    i.PropertyChanged += ClassInfoOnPropertyChanged;
                }

                break;
            }
        }
    }

    private void TimeLayoutOnLayoutItemChanged(object? sender, TimeLayoutUpdateEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.AddIndexClasses == -1)
                    break;
                foreach (var i in e.AddedItems)
                {
                    Classes.Insert(e.AddIndexClasses, new ClassInfo());
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.RemoveIndexClasses == -1)
                    break;
                foreach (var i in e.RemovedItems)
                {
                    Classes.RemoveAt(e.RemoveIndexClasses);
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                break;
            case NotifyCollectionChangedAction.Move:
                break;
            case NotifyCollectionChangedAction.Reset:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void LayoutsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems == null)
                    return;
                //foreach (var i in e.NewItems)
                //{
                //    var index = e.NewStartingIndex;
                //    Classes.Insert(index, new ClassInfo()
                //    {

                //    });
                //}
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems == null)
                    return;
                //foreach (var i in e.OldItems)
                //{
                //    Classes.RemoveAt(e.OldStartingIndex);
                //}
                break;
            case NotifyCollectionChangedAction.Replace:
                break;
            case NotifyCollectionChangedAction.Move:
                break;
            case NotifyCollectionChangedAction.Reset:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        //RefreshClassesList();
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

    public void RefreshClassesList(bool isDiffMode=false)
    {
        //App.GetService<ILogger<ClassPlan>>().LogTrace("Calling Refresh ClassesList: \n{}", new StackTrace());
        // 对齐长度
        if (TimeLayoutId == null || !TimeLayouts.ContainsKey(TimeLayoutId))
        {
            return;
        }
        
        var c = (from i in TimeLayout.Layouts where i.TimeType == 0 select i).ToList();
        var l = c.Count;
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
            if (Classes[i].SubjectId == "" && Classes[i].CurrentTimeLayoutItem.DefaultClassId != "")
            {
                Classes[i].SubjectId = Classes[i].CurrentTimeLayoutItem.DefaultClassId;
            }
        }

        LastTimeLayoutCount = TimeLayout.Layouts.Count;
    }

    public void RemoveTimePointSafe(TimeLayoutItem timePoint)
    {
        foreach (var i in from i in Classes where i.CurrentTimeLayoutItem == timePoint select i)
        {
            Classes.Remove(i);
        }
        RefreshClassesList();
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

    public bool IsOverlay
    {
        get => _isOverlay;
        set
        {
            if (value == _isOverlay) return;
            _isOverlay = value;
            OnPropertyChanged();
        }
    }

    public string? OverlaySourceId
    {
        get => _overlaySourceId;
        set
        {
            if (value == _overlaySourceId) return;
            _overlaySourceId = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public ClassPlan? OverlaySource
    {
        get => _overlaySource;
        set
        {
            if (Equals(value, _overlaySource)) return;
            _overlaySource = value;
            OnPropertyChanged();
        }
    }

    public DateTime OverlaySetupTime
    {
        get => _overlaySetupTime;
        set
        {
            if (value.Equals(_overlaySetupTime)) return;
            _overlaySetupTime = value;
            OnPropertyChanged();
        }
    }

    public void SetupOverlay(ClassPlan source)
    {
        if (!IsOverlay)
        {
            return;
        }
        OverlaySource = source;
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (value == _isEnabled) return;
            _isEnabled = value;
            OnPropertyChanged();
        }
    }
}