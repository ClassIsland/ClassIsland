using System.ComponentModel;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 精确时间服务，可以从此服务获取来自NTP服务器的精确时间。
/// </summary>
public interface IExactTimeService : INotifyPropertyChanged, INotifyPropertyChanging
{
    /// <summary>
    /// 时间同步状态信息
    /// </summary>
    string SyncStatusMessage { get; set; }
    
    /// <summary>
    /// 立刻同步时间。
    /// </summary>
    void Sync();
    
    /// <summary>
    /// 获取当前精确当地时间。
    /// </summary>
    /// <returns>当前时间</returns>
    DateTime GetCurrentLocalDateTime();
}