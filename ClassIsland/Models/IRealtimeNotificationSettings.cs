using System.ComponentModel;

namespace ClassIsland.Models;

public interface IRealtimeNotificationSettings : INotifyPropertyChanged
{
    bool IsUpcomingClassRealtimeNotificationEnabled { get; set; }
    int UpcomingClassRealtimeNotificationLeadMinutes { get; set; }
    bool UpcomingClassRealtimeNotificationShowSubjectName { get; set; }
    bool UpcomingClassRealtimeNotificationShowRemainingTime { get; set; }
    bool UpcomingClassRealtimeNotificationShowTimeRange { get; set; }
    bool UpcomingClassRealtimeNotificationShowLocation { get; set; }
    bool UpcomingClassRealtimeNotificationShowTeacherName { get; set; }
    bool IsCurrentClassRealtimeNotificationEnabled { get; set; }
    bool CurrentClassRealtimeNotificationShowSubjectName { get; set; }
    bool CurrentClassRealtimeNotificationShowRemainingTime { get; set; }
    bool CurrentClassRealtimeNotificationShowTimeRange { get; set; }
    bool CurrentClassRealtimeNotificationShowLocation { get; set; }
    bool CurrentClassRealtimeNotificationShowTeacherName { get; set; }
}
