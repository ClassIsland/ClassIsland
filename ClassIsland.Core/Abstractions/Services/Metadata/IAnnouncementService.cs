using System.Collections.ObjectModel;
using System.ComponentModel;
using ClassIsland.Core.Models.Metadata.Announcement;

namespace ClassIsland.Core.Abstractions.Services.Metadata;

/// <summary>
/// 应用内公告服务
/// </summary>
public interface IAnnouncementService : INotifyPropertyChanged
{
    /// <summary>
    /// 公告列表
    /// </summary>
    IReadOnlyList<Announcement> Announcements { get; }

    /// <summary>
    /// 刷新公告信息。
    /// </summary>
    /// <returns></returns>
    Task RefreshAnnouncements();

    /// <summary>
    /// 本地存储的已读公告
    /// </summary>
    ObservableCollection<Guid> ReadAnnouncementsLocal { get; }

    /// <summary>
    /// 机器存储的已读公告
    /// </summary>
    ObservableCollection<Guid> ReadAnnouncementsMachine { get; }
}