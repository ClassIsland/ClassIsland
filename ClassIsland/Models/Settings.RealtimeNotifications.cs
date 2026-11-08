using System;

namespace ClassIsland.Models;

public partial class Settings : IRealtimeNotificationSettings
{
    private bool _isUpcomingClassRealtimeNotificationEnabled = true;

    public bool IsUpcomingClassRealtimeNotificationEnabled
    {
        get => _isUpcomingClassRealtimeNotificationEnabled;
        set => SetProperty(ref _isUpcomingClassRealtimeNotificationEnabled, value);
    }

    private int _upcomingClassRealtimeNotificationLeadMinutes = 20;

    public int UpcomingClassRealtimeNotificationLeadMinutes
    {
        get => _upcomingClassRealtimeNotificationLeadMinutes;
        set => SetProperty(ref _upcomingClassRealtimeNotificationLeadMinutes, Math.Clamp(value, 0, 1440));
    }

    private bool _upcomingClassRealtimeNotificationShowSubjectName = true;

    public bool UpcomingClassRealtimeNotificationShowSubjectName
    {
        get => _upcomingClassRealtimeNotificationShowSubjectName;
        set => SetProperty(ref _upcomingClassRealtimeNotificationShowSubjectName, value);
    }

    private bool _upcomingClassRealtimeNotificationShowRemainingTime = true;

    public bool UpcomingClassRealtimeNotificationShowRemainingTime
    {
        get => _upcomingClassRealtimeNotificationShowRemainingTime;
        set => SetProperty(ref _upcomingClassRealtimeNotificationShowRemainingTime, value);
    }

    private bool _upcomingClassRealtimeNotificationShowTimeRange = false;

    public bool UpcomingClassRealtimeNotificationShowTimeRange
    {
        get => _upcomingClassRealtimeNotificationShowTimeRange;
        set => SetProperty(ref _upcomingClassRealtimeNotificationShowTimeRange, value);
    }

    private bool _upcomingClassRealtimeNotificationShowLocation = true;

    public bool UpcomingClassRealtimeNotificationShowLocation
    {
        get => _upcomingClassRealtimeNotificationShowLocation;
        set => SetProperty(ref _upcomingClassRealtimeNotificationShowLocation, value);
    }

    private bool _upcomingClassRealtimeNotificationShowTeacherName = false;

    public bool UpcomingClassRealtimeNotificationShowTeacherName
    {
        get => _upcomingClassRealtimeNotificationShowTeacherName;
        set => SetProperty(ref _upcomingClassRealtimeNotificationShowTeacherName, value);
    }

    private bool _isCurrentClassRealtimeNotificationEnabled = true;

    public bool IsCurrentClassRealtimeNotificationEnabled
    {
        get => _isCurrentClassRealtimeNotificationEnabled;
        set => SetProperty(ref _isCurrentClassRealtimeNotificationEnabled, value);
    }

    private bool _currentClassRealtimeNotificationShowSubjectName = true;

    public bool CurrentClassRealtimeNotificationShowSubjectName
    {
        get => _currentClassRealtimeNotificationShowSubjectName;
        set => SetProperty(ref _currentClassRealtimeNotificationShowSubjectName, value);
    }

    private bool _currentClassRealtimeNotificationShowRemainingTime = true;

    public bool CurrentClassRealtimeNotificationShowRemainingTime
    {
        get => _currentClassRealtimeNotificationShowRemainingTime;
        set => SetProperty(ref _currentClassRealtimeNotificationShowRemainingTime, value);
    }

    private bool _currentClassRealtimeNotificationShowTimeRange = false;

    public bool CurrentClassRealtimeNotificationShowTimeRange
    {
        get => _currentClassRealtimeNotificationShowTimeRange;
        set => SetProperty(ref _currentClassRealtimeNotificationShowTimeRange, value);
    }

    private bool _currentClassRealtimeNotificationShowLocation = false;

    public bool CurrentClassRealtimeNotificationShowLocation
    {
        get => _currentClassRealtimeNotificationShowLocation;
        set => SetProperty(ref _currentClassRealtimeNotificationShowLocation, value);
    }

    private bool _currentClassRealtimeNotificationShowTeacherName = false;

    public bool CurrentClassRealtimeNotificationShowTeacherName
    {
        get => _currentClassRealtimeNotificationShowTeacherName;
        set => SetProperty(ref _currentClassRealtimeNotificationShowTeacherName, value);
    }
}
