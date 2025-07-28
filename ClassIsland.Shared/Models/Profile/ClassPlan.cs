using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Serialization;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.JsonConverters;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 代表一个课表。
/// </summary>
public class ClassPlan : AttachableSettingsObject
{
    private Guid _timeLayoutId = Guid.Empty;
    private ObservableCollection<ClassInfo> _classes = new();
    private string _name = "新课表";
    private ObservableDictionary<Guid, TimeLayout> _timeLayouts = new();
    private ObservableDictionary<Guid, ClassPlan> _classPlans = new();
    private TimeRule _timeRule = new();
    private bool _isActivated = false;
    private bool _isOverlay = false;
    private Guid? _overlaySourceId;
    private bool _isEnabled = true;
    private DateTime _overlaySetupTime = DateTime.Now;
    private int _lastTimeLayoutCount = -1;
    private Guid _associatedGroup = ClassPlanGroup.DefaultGroupGuid;

    private bool _isValidTimeLayoutItemsDirty = true;
    private ObservableCollection<TimeLayoutItem> _validTimeLayoutItems = [];

    /// <summary>
    /// 有效的时间点列表
    /// </summary>
    [JsonIgnore]
    public ObservableCollection<TimeLayoutItem> ValidTimeLayoutItems
    {
        get
        {
            if (!_isValidTimeLayoutItemsDirty) 
                return _validTimeLayoutItems;
            _validTimeLayoutItems = GetValidTimeLayoutItems();
            _isValidTimeLayoutItemsDirty = false;
            return _validTimeLayoutItems;
        }
    }

    private void MakeValidTimeLayoutItemsDirty()
    {
        _isValidTimeLayoutItemsDirty = true;
        OnPropertyChanged(nameof(ValidTimeLayoutItems));
    }

    private ObservableCollection<TimeLayoutItem> GetValidTimeLayoutItems()
    {
        if (TimeLayout == null)
            return [];
        var timeLayoutMap = Classes.ToDictionary(x => x.CurrentTimeLayoutItem, x => x);
        var displayTimePoints = TimeLayout.Layouts
            .Where(x => x.TimeType is 0 or 1 or 2)
            .ToList();
        ObservableCollection<TimeLayoutItem> items = [.. displayTimePoints.Select(x => x)];
        List<TimeLayoutItem> remove = [];
        // 正向搜索
        var isPrevEnabled = true;
        for (var i = 0; i < displayTimePoints.Count; i++)
        {
            if (timeLayoutMap.TryGetValue(items[i], out var info))
            {
                isPrevEnabled = info.IsEnabled;
            }

            if (!isPrevEnabled)
            {
                remove.Add(displayTimePoints[i]);
            }
        }
        // 反向搜索
        isPrevEnabled = true;
        for (var i = displayTimePoints.Count - 1; i >= 0; i--)
        {
            if (timeLayoutMap.TryGetValue(items[i], out var info))
            {
                isPrevEnabled = info.IsEnabled;
            }

            if (!isPrevEnabled)
            {
                remove.Add(displayTimePoints[i]);
            }
        }

        foreach (var i in remove)
        {
            items.Remove(i);
        }
        return items;
    }

    /// <summary>
    /// 当课程表更新时触发
    /// </summary>
    public event EventHandler? ClassesChanged;


    private void NotifyClassesChanged()
    {
        ClassesChanged?.Invoke(this, EventArgs.Empty);
    }

    [JsonIgnore]
    internal int LastTimeLayoutCount
    {
        get => _lastTimeLayoutCount;
        set
        {
            if (value == _lastTimeLayoutCount) return;
            _lastTimeLayoutCount = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 实例化对象
    /// </summary>
    public ClassPlan()
    {
        PropertyChanged += OnPropertyChanged;
        PropertyChanging += OnPropertyChanging;
        if (TimeLayouts.ContainsKey(TimeLayoutId) && TimeLayout != null)
        {
            TimeLayout.LayoutObjectChanged += TimeLayoutOnLayoutObjectChanged;
            TimeLayout.Layouts.CollectionChanged += LayoutsOnCollectionChanged;
            TimeLayout.LayoutItemChanged += TimeLayoutOnLayoutItemChanged;
        }
        Classes.CollectionChanged += ClassesOnCollectionChanged;
        ClassesChanged += (sender, args) => MakeValidTimeLayoutItemsDirty();
    }

    private void OnPropertyChanging(object? sender, PropertyChangingEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(TimeLayout):
            {
                if (TimeLayout != null)
                {
                    TimeLayout.LayoutObjectChanged -= TimeLayoutOnLayoutObjectChanged;
                    TimeLayout.LayoutItemChanged -= TimeLayoutOnLayoutItemChanged;
                    TimeLayout.Layouts.CollectionChanged -= LayoutsOnCollectionChanged;
                }

                NotifyClassesChanged();
                break;
            }
            case nameof(Classes):
            {
                Classes.CollectionChanged -= ClassesOnCollectionChanged;
                foreach (var i in Classes)
                {
                    i.PropertyChanged -= ClassInfoOnPropertyChanged;
                }

                break;
            }
        }
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

        if (!IsActivated) return;

        // TODO: 支持预定临时层后，移除 IsActivated 判断。
        if (e.PropertyName is not "IsChangedClass")
            RefreshIsChangedClass();
    }

    private void TimeLayoutOnLayoutObjectChanged(object? sender, EventArgs e)
    {
        if (TimeLayout != null)
        {
            TimeLayout.Layouts.CollectionChanged += LayoutsOnCollectionChanged;
        }
        RefreshClassesList(true);
        NotifyClassesChanged();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(TimeLayout):
            // case nameof(TimeLayoutId):
            {
                if (TimeLayout != null)
                {
                    RefreshClassesList();
                    TimeLayout.LayoutObjectChanged -= TimeLayoutOnLayoutObjectChanged;
                    TimeLayout.LayoutItemChanged -= TimeLayoutOnLayoutItemChanged;
                    TimeLayout.Layouts.CollectionChanged -= LayoutsOnCollectionChanged;
                    TimeLayout.LayoutObjectChanged += TimeLayoutOnLayoutObjectChanged;
                    TimeLayout.LayoutItemChanged += TimeLayoutOnLayoutItemChanged;
                    TimeLayout.Layouts.CollectionChanged += LayoutsOnCollectionChanged;
                }

                NotifyClassesChanged();
                break;
            }
            case nameof(Classes):
            {
                Classes.CollectionChanged -= ClassesOnCollectionChanged;
                foreach (var i in Classes)
                {
                    i.PropertyChanged -= ClassInfoOnPropertyChanged;
                }
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
                if (e.AddIndexClasses < 0 || e.AddIndexClasses > Classes.Count)
                    break;
                foreach (var i in e.AddedItems)
                {
                    var classInfo = new ClassInfo();
                    if (TimeLayout != null)
                    {
                        classInfo.CurrentTimeLayout = TimeLayout;
                        classInfo.Index = e.AddIndexClasses;
                    }
                    Classes.Insert(e.AddIndexClasses, classInfo);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.RemoveIndexClasses < 0 || e.RemoveIndexClasses >= Classes.Count)
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
    internal ObservableDictionary<Guid, ClassPlan> ClassPlans
    {
        get => _classPlans;
        set
        {
            if (Equals(value, _classPlans)) return;
            _classPlans = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    internal ObservableDictionary<Guid, TimeLayout> TimeLayouts
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

    /// <summary>
    /// 当前课表的时间表
    /// </summary>
    [JsonIgnore]
    public TimeLayout? TimeLayout =>
#if NETCOREAPP
        TimeLayouts.GetValueOrDefault(TimeLayoutId);
#else
        TimeLayouts.TryGetValue(TimeLayoutId, out var value) ? value : null;   
#endif

    /// <summary>
    /// 当前课表的时间表ID
    /// </summary>
    [JsonConverter(typeof(GuidEmptyFallbackConverter))]
    public Guid TimeLayoutId
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

    /// <summary>
    /// 课表触发规则
    /// </summary>
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

    /// <summary>
    /// 课表包含的课程信息
    /// </summary>
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

    /// <summary>
    /// 课表名称
    /// </summary>
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

    internal void RefreshClassesList(bool isDiffMode=false)
    {
        //App.GetService<ILogger<ClassPlan>>().LogTrace("Calling Refresh ClassesList: \n{}", new StackTrace());
        // 对齐长度
        if (TimeLayout == null)
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
            if (Classes[i].SubjectId == Guid.Empty && Classes[i].CurrentTimeLayoutItem.DefaultClassId != Guid.Empty)
            {
                Classes[i].SubjectId = Classes[i].CurrentTimeLayoutItem.DefaultClassId;
            }
        }

        LastTimeLayoutCount = TimeLayout.Layouts.Count;
    }

    /// <summary>
    /// 标记换课课程
    /// </summary>
    internal void RefreshIsChangedClass()
    {
        if (OverlaySourceId == null ||
            !ClassPlans.TryGetValue(OverlaySourceId ?? Guid.Empty, out var overlaySource) ||
            Classes.Count != overlaySource.Classes.Count)
        {
            foreach (var classInfo in Classes)
                classInfo.IsChangedClass = false;
            return;
        }

        foreach (var classInfo in Classes)
        {
            classInfo.IsChangedClass =
                classInfo.Index >= overlaySource.Classes.Count ? false : // 兜底判断，理论上可以删去
                classInfo.SubjectId != overlaySource.Classes[classInfo.Index].SubjectId;
        }
    }

    internal void RemoveTimePointSafe(TimeLayoutItem timePoint)
    {
        foreach (var i in from i in Classes where i.CurrentTimeLayoutItem == timePoint select i)
        {
            Classes.Remove(i);
        }
        RefreshClassesList();
    }

    /// <summary>
    /// 课表是否被激活（正在使用）
    /// </summary>
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

    /// <summary>
    /// 是否是临时层课表
    /// </summary>
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

    /// <summary>
    /// 临时层课表对应的源课表ID
    /// </summary>
    public Guid? OverlaySourceId
    {
        get => _overlaySourceId;
        set
        {
            if (value == _overlaySourceId) return;
            _overlaySourceId = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 临时层设置时间
    /// </summary>
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

    /// <summary>
    /// 是否默认启用
    /// </summary>
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

    /// <summary>
    /// 该课表关联的课表群。
    /// </summary>
    public Guid AssociatedGroup
    {
        get => _associatedGroup;
        set
        {
            if (value == _associatedGroup) return;
            _associatedGroup = value;
            OnPropertyChanged();
        }
    }
}
