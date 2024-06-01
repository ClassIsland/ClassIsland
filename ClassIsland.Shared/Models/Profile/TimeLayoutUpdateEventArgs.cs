using System.Collections.Specialized;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 时间表更新事件参数
/// </summary>
public class TimeLayoutUpdateEventArgs
{
    /// <summary>
    /// 更新操作
    /// </summary>
    public NotifyCollectionChangedAction Action { get; set; }

    /// <summary>
    /// 添加的元素
    /// </summary>
    public List<TimeLayoutItem> AddedItems { get; set; } = new();

    /// <summary>
    /// 删除的元素
    /// </summary>
    public List<TimeLayoutItem> RemovedItems { get; set; } = new();

    /// <summary>
    /// 添加元素的索引
    /// </summary>
    public int AddIndex { get; set; } = -1;

    /// <summary>
    /// 删除元素的索引
    /// </summary>
    public int RemoveIndex { get; set; } = -1;

    /// <summary>
    /// 添加元素在课表中的索引
    /// </summary>
    public int AddIndexClasses { get; set; } = -1;

    /// <summary>
    /// 删除元素在课表中的索引
    /// </summary>
    public int RemoveIndexClasses { get; set; } = -1;
}