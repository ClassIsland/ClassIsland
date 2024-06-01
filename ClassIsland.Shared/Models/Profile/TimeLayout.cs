using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 代表一个时间表
/// </summary>
public class TimeLayout : AttachableSettingsObject
{
    private ObservableCollection<TimeLayoutItem> _layouts = new();
    private string _name = "新时间表";
    private bool _isActivated = false;
    private bool _isActivatedManually = false;

    /// <summary>
    /// 初始化对象
    /// </summary>
    public TimeLayout()
    {
        PropertyChanged += OnPropertyChanged;
        Layouts.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(Layouts));
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

    /// <summary>
    /// 在指定索引处插入时间点
    /// </summary>
    /// <param name="index">插入时间点的索引</param>
    /// <param name="item">要插入的时间点</param>
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

    /// <summary>
    /// 删除指定的时间点
    /// </summary>
    /// <param name="item">要删除的时间点</param>
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
            _layouts = value;
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