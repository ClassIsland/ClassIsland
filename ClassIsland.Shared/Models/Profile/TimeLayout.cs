using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 代表一个时间表
/// </summary>
public class TimeLayout : AttachableSettingsObject
{
    private ObservableCollection<TimeLayoutItem> _layouts = new();
    private readonly Dictionary<TimeLayoutItem, int> _timeTypeChangeClassIndexes = new();
    private string _name = "新时间表";
    private bool _isActivated = false;
    private bool _isActivatedManually = false;
    private bool _isOverlay = false;
    private Guid? _overlaySourceId;

    /// <summary>
    /// 是否是临时层时间表
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
    /// 临时层时间表对应的源时间表ID
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
    /// 初始化对象
    /// </summary>
    public TimeLayout()
    {
        PropertyChanged += OnPropertyChanged;
        Layouts.CollectionChanged += LayoutsOnCollectionChanged;
        AttachLayoutItems(Layouts);
    }

    /// <summary>
    /// 时间表对象被更改事件
    /// </summary>
    public event EventHandler? LayoutObjectChanged;

    /// <summary>
    /// 时间表元素被更改事件
    /// </summary>
    public event EventHandler<TimeLayoutUpdateEventArgs>? LayoutItemChanged;

    internal void SortCompleted()
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

    private void LayoutsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<TimeLayoutItem>())
            {
                item.PropertyChanging -= TimeLayoutItemOnPropertyChanging;
                item.PropertyChanged -= TimeLayoutItemOnPropertyChanged;
                _timeTypeChangeClassIndexes.Remove(item);
            }
        }

        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<TimeLayoutItem>())
            {
                item.PropertyChanging += TimeLayoutItemOnPropertyChanging;
                item.PropertyChanged += TimeLayoutItemOnPropertyChanged;
            }
        }

        OnPropertyChanged(nameof(Layouts));
    }

    private void AttachLayoutItems(IEnumerable<TimeLayoutItem> items)
    {
        foreach (var item in items)
        {
            item.PropertyChanging += TimeLayoutItemOnPropertyChanging;
            item.PropertyChanged += TimeLayoutItemOnPropertyChanged;
        }
    }

    private void DetachLayoutItems(IEnumerable<TimeLayoutItem> items)
    {
        foreach (var item in items)
        {
            item.PropertyChanging -= TimeLayoutItemOnPropertyChanging;
            item.PropertyChanged -= TimeLayoutItemOnPropertyChanged;
            _timeTypeChangeClassIndexes.Remove(item);
        }
    }

    private void TimeLayoutItemOnPropertyChanging(object? sender, PropertyChangingEventArgs e)
    {
        if (sender is not TimeLayoutItem item || e.PropertyName != nameof(TimeLayoutItem.TimeType))
        {
            return;
        }

        _timeTypeChangeClassIndexes[item] = GetClassIndex(item);
    }

    private void TimeLayoutItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is TimeLayoutItem item && e.PropertyName == nameof(TimeLayoutItem.TimeType))
        {
            NotifyTimeLayoutItemTypeChanged(item);
            return;
        }

        LayoutObjectChanged?.Invoke(this, EventArgs.Empty);
    }

    private void NotifyTimeLayoutItemTypeChanged(TimeLayoutItem item)
    {
        if (!_timeTypeChangeClassIndexes.TryGetValue(item, out var removeIndexClasses))
        {
            removeIndexClasses = GetClassIndex(item);
        }
        else
        {
            _timeTypeChangeClassIndexes.Remove(item);
        }

        LayoutItemChanged?.Invoke(this, new TimeLayoutUpdateEventArgs()
        {
            Action = NotifyCollectionChangedAction.Replace,
            AddedItems = { item },
            RemovedItems = { item },
            AddIndex = Layouts.IndexOf(item),
            RemoveIndex = Layouts.IndexOf(item),
            AddIndexClasses = GetClassIndex(item),
            RemoveIndexClasses = removeIndexClasses
        });
    }

    private int GetClassIndex(TimeLayoutItem item)
    {
        if (item.TimeType != 0)
        {
            return -1;
        }

        return Layouts.Where(x => x.TimeType == 0).ToList().IndexOf(item);
    }
    /// <summary>
    /// 在指定索引处插入时间点
    /// </summary>
    /// <param name="index">插入时间点的索引</param>
    /// <param name="item">要插入的时间点</param>
    public void InsertTimePoint(int index, TimeLayoutItem item)
    {
        Layouts.Insert(index, item);
        NotifyTimeLayoutItemAdded(index, item);
    }

    internal void NotifyTimeLayoutItemAdded(int index, TimeLayoutItem item)
    {
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

    /// <summary>
    /// 删除指定的时间点
    /// </summary>
    /// <param name="item">要删除的时间点</param>
    public void RemoveTimePoint(TimeLayoutItem item)
    {
        var index = Layouts.IndexOf(item);
        NotifyTimeLayoutItemRemoved(index, item);
        Layouts.Remove(item);
    }

    internal void NotifyTimeLayoutItemRemoved(int index, TimeLayoutItem item)
    {
        var ci = -1;
        if (item.TimeType == 0)
        {
            ci = (from i in Layouts where i.TimeType==0 select i).ToList().IndexOf(item);
        }
        LayoutItemChanged?.Invoke(this, new TimeLayoutUpdateEventArgs()
        {
            Action = NotifyCollectionChangedAction.Remove,
            RemovedItems = { item },
            RemoveIndex = index,
            RemoveIndexClasses = ci
        });
    }

    /// <summary>
    /// 时间表名称
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

    /// <summary>
    /// 时间表内容
    /// </summary>
    public ObservableCollection<TimeLayoutItem> Layouts
    {
        get => _layouts;
        set
        {
            if (Equals(value, _layouts)) return;
            _layouts.CollectionChanged -= LayoutsOnCollectionChanged;
            DetachLayoutItems(_layouts);
            _layouts = value;
            _layouts.CollectionChanged += LayoutsOnCollectionChanged;
            AttachLayoutItems(_layouts);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 时间表是否处于激活（正在使用）状态
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
    /// 时间表是否是被手动激活
    /// </summary>
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
