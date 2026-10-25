using System.Collections.ObjectModel;
using System.ComponentModel;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Models.NotificationProviderSettings;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Notification;
using ClassIsland.Shared.Models.Profile;
using NotificationRequest = ClassIsland.Core.Models.Notification.NotificationRequest;

namespace ClassIsland.Lessons.Tests;

// 这些接口包含内部成员，使用测试程序集内的实现，避免动态代理程序集的访问限制。
internal sealed class TestLessonsService : ILessonsService
{
    public Func<DateTime, ClassPlan?> GetPlan { get; init; } = _ => null;
    public ClassInfo? CurrentClassInfo { get; set; }
    public ClassInfo? NextClassInfo { get; set; }
    public ClassPlan? CurrentClassPlan { get; set; }
    public int CurrentSelectedIndex { get; set; }
    public Subject NextClassSubject { get; set; } = Subject.Fallback;
    public TimeLayoutItem NextBreakingTimeLayoutItem { get; set; } = TimeLayoutItem.Empty;
    public TimeLayoutItem NextClassTimeLayoutItem { get; set; } = TimeLayoutItem.Empty;
    public TimeSpan OnClassLeftTime { get; set; }
    public TimeSpan OnBreakingTimeLeftTime { get; set; }
    public TimeState CurrentState { get; set; }
    public TimeLayoutItem CurrentTimeLayoutItem { get; set; } = TimeLayoutItem.Empty;
    public Subject? CurrentSubject { get; set; }
    public bool IsClassPlanEnabled { get; set; } = true;
    public bool IsClassPlanLoaded { get; set; } = true;
    public bool IsLessonConfirmed { get; set; }
    public bool IsTimerRunning => false;
    TimeState ILessonsService.CurrentOverlayStatus { get; set; }
    public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }
    public event PropertyChangingEventHandler? PropertyChanging { add { } remove { } }
    public event EventHandler? PreMainTimerTicked { add { } remove { } }
    public event EventHandler? PostMainTimerTicked { add { } remove { } }
    public event EventHandler? OnClass { add { } remove { } }
    public event EventHandler? OnBreakingTime { add { } remove { } }
    public event EventHandler? OnAfterSchool { add { } remove { } }
    public event EventHandler? CurrentTimeStateChanged { add { } remove { } }
    public event EventHandler<Predicate<DateOnly>>? ScheduleItemsInvalidated { add { } remove { } }
    public ClassPlan? GetClassPlanByDate(DateTime date) => GetPlan(date);
    public ClassPlan? GetClassPlanByDate(DateTime date, out Guid? guid)
    {
        guid = null;
        return GetPlan(date);
    }
    public ObservableCollection<int> GetCyclePositionsByDate(DateTime? referenceTime = null) => throw new NotSupportedException();
    public OrderedDictionary<Guid, ScheduleItem> GetScheduleItemsByDate(DateOnly date) => throw new NotSupportedException();
    public void StartMainTimer() => throw new NotSupportedException();
    public void StopMainTimer() => throw new NotSupportedException();
    void ILessonsService.DebugTriggerOnClass() => throw new NotSupportedException();
    void ILessonsService.DebugTriggerOnBreakingTime() => throw new NotSupportedException();
    void ILessonsService.DebugTriggerOnAfterSchool() => throw new NotSupportedException();
    void ILessonsService.DebugTriggerOnStateChanged() => throw new NotSupportedException();
}

internal sealed class TestProfileService : IProfileService
{
    public string CurrentProfilePath { get; set; } = "Default.json";
    public Profile Profile { get; set; } = new();
    public bool IsCurrentProfileTrusted => true;
    public void CleanExpiredTempClassPlan() => throw new NotSupportedException();
    public void ClearExpiredTempClassPlanGroup() => throw new NotSupportedException();
    Task IProfileService.LoadProfileAsync() => throw new NotSupportedException();
    public void SaveProfile() => throw new NotSupportedException();
    public void SaveProfile(string filename) => throw new NotSupportedException();
    public Guid? CreateTempClassPlan(Guid id, Guid? timeLayoutId = null, DateTime? enableDateTime = null) => throw new NotSupportedException();
    public Guid? CreateTempClassPlan(Guid id, Guid? timeLayoutId, DateTime? enableDateTime, bool createTempTimeLayout) => throw new NotSupportedException();
    public Guid? ForkAndReplaceScheduleItem(Guid scheduleItemId, DateTime date) => throw new NotSupportedException();
    public bool ConvertScheduleItemReplacementToNormal(Guid scheduleItemId) => throw new NotSupportedException();
    public void ClearTempClassPlan() => throw new NotSupportedException();
    public void ConvertToStdClassPlan() => throw new NotSupportedException();
    public void ConvertToStdClassPlan(Guid id) => throw new NotSupportedException();
    public void SetupTempClassPlanGroup(Guid key, DateTime? expireTime = null) => throw new NotSupportedException();
    public void ClearTempClassPlanGroup() => throw new NotSupportedException();
    public void TrustCurrentProfile() => throw new NotSupportedException();
}

internal sealed class TestNotificationHostService(ClassNotificationSettings settings) : INotificationHostService
{
    public T GetNotificationProviderSettings<T>(Guid id) where T : class =>
        settings as T ?? throw new NotSupportedException(typeof(T).Name);
    public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }
    public bool IsNotificationsPlaying => false;
    PriorityQueue<NotificationRequest, NotificationPriority> INotificationHostService.RequestQueue => throw new NotSupportedException();
    ObservableCollection<NotificationProviderRegisterInfo> INotificationHostService.NotificationProviders => throw new NotSupportedException();
    NotificationRequest? INotificationHostService.CurrentRequest { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    NotificationRequest INotificationHostService.GetRequest() => throw new NotSupportedException();
    public void RegisterNotificationProvider(INotificationProvider provider) => throw new NotSupportedException();
    void INotificationHostService.ShowNotification(NotificationRequest request, Guid providerGuid, Guid channelGuid, bool pushNotifications, bool isPlayed) => throw new NotSupportedException();
    Task INotificationHostService.ShowNotificationAsync(NotificationRequest request, Guid providerGuid, Guid channelGuid) => throw new NotSupportedException();
    void INotificationHostService.ShowChainedNotifications(NotificationRequest[] requests, Guid providerGuid, Guid channelGuid) => throw new NotSupportedException();
    Task INotificationHostService.ShowChainedNotificationsAsync(NotificationRequest[] requests, Guid providerGuid, Guid channelGuid) => throw new NotSupportedException();
    void INotificationHostService.RegisterNotificationChannel(NotificationChannel channel) => throw new NotSupportedException();
    public void WriteNotificationProviderSettings<T>(Guid id, T value) => throw new NotSupportedException();
    void INotificationHostService.CancelAllNotifications() => throw new NotSupportedException();
    public void RegisterNotificationConsumer(INotificationConsumer consumer, int priority) => throw new NotSupportedException();
    public void UnregisterNotificationConsumer(INotificationConsumer consumer) => throw new NotSupportedException();
    public IList<NotificationPlayingTicket> PullNotificationRequests() => throw new NotSupportedException();
    public Task StartAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task StopAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
}
