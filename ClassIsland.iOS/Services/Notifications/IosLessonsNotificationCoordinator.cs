using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums;
using ClassIsland.iOS.Services.Platform;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;
using Foundation;
using UIKit;

namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// 在启动和前后台切换时滚动同步课程本地通知；通知提交后不依赖应用继续运行。
/// </summary>
internal sealed class IosLessonsNotificationCoordinator : IDisposable
{
    private static readonly TimeSpan RefreshDebounceInterval = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan AttachedSettingsScanInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RollingScheduleRefreshInterval = TimeSpan.FromHours(6);

    private readonly IosNotificationAuthorizationService _authorizationService;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly IosLessonNotificationScheduler _scheduler = new();
    private readonly DispatcherTimer _refreshDebounceTimer;
    private readonly DispatcherTimer _attachedSettingsScanTimer;
    private readonly DispatcherTimer _rollingScheduleRefreshTimer;
    private readonly HashSet<INotifyPropertyChanged> _propertyChangeSources = [];
    private readonly HashSet<INotifyPropertyChanged> _attachedSettingsSources = [];
    private readonly HashSet<INotifyCollectionChanged> _collectionChangeSources = [];
    private readonly HashSet<ClassPlan> _classPlanChangeSources = [];
    private readonly HashSet<TimeLayout> _timeLayoutChangeSources = [];
    private NSObject? _foregroundObserver;
    private NSObject? _backgroundObserver;
    private ILessonsService? _lessonsService;
    private IProfileService? _profileService;
    private IExactTimeService? _exactTimeService;
    private SettingsService? _settingsService;
    private INotificationHostService? _notificationHostService;
    private IosLessonNotificationScheduleFactory? _scheduleFactory;
    private readonly IosNotificationQueueConsumer _queueConsumer = new();
    private IReadOnlyList<IosLessonNotificationRequest>? _lastRequests;
    private bool? _lastAuthorizationState;
    private int _refreshPending;
    private bool _isStarted;
    private bool _isWorkStarted;
    private int _backgroundRefreshActive;

    public IosLessonsNotificationCoordinator(
        IosNotificationAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
        _refreshDebounceTimer = new DispatcherTimer
        {
            Interval = RefreshDebounceInterval
        };
        _refreshDebounceTimer.Tick += RefreshDebounceTimerOnTick;
        _attachedSettingsScanTimer = new DispatcherTimer
        {
            Interval = AttachedSettingsScanInterval
        };
        _attachedSettingsScanTimer.Tick += AttachedSettingsScanTimerOnTick;
        _rollingScheduleRefreshTimer = new DispatcherTimer
        {
            Interval = RollingScheduleRefreshInterval
        };
        _rollingScheduleRefreshTimer.Tick += RollingScheduleRefreshTimerOnTick;
    }

    public void Start()
    {
        if (_isStarted)
        {
            return;
        }

        _isStarted = true;
        AppBase.Current.AppStarted += OnAppStarted;
        _foregroundObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            UIApplication.WillEnterForegroundNotification,
            _ => QueueRefresh());
        _backgroundObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            UIApplication.DidEnterBackgroundNotification,
            _ => QueueBackgroundRefresh());

        if (AppBase.CurrentLifetime == ApplicationLifetime.Running)
        {
            StartWork();
        }
    }

    private void OnAppStarted(object? sender, EventArgs e) => StartWork();

    private void StartWork()
    {
        if (_isWorkStarted || _cancellation.IsCancellationRequested)
        {
            return;
        }

        _lessonsService = IAppHost.GetService<ILessonsService>();
        _profileService = IAppHost.GetService<IProfileService>();
        _notificationHostService = IAppHost.GetService<INotificationHostService>();
        _exactTimeService = IAppHost.GetService<IExactTimeService>();
        _settingsService = IAppHost.GetService<SettingsService>();
        _scheduleFactory = new IosLessonNotificationScheduleFactory(
            _lessonsService,
            _profileService,
            _notificationHostService,
            _settingsService,
            _exactTimeService);
        _notificationHostService.RegisterNotificationConsumer(
            _queueConsumer,
            int.MinValue);
        _isWorkStarted = true;
        _lessonsService.PropertyChanged += LessonsServiceOnPropertyChanged;
        _exactTimeService.PropertyChanged += ExactTimeServiceOnPropertyChanged;
        PlatformServices.SystemEventsService.TimeChanged += SystemEventsOnTimeChanged;
        RebuildChangeSubscriptions();
        _attachedSettingsScanTimer.Start();
        _rollingScheduleRefreshTimer.Start();
        QueueRefresh();
    }

    private void QueueRefresh() => _ = QueueRefreshAsync();

    private async Task QueueRefreshAsync()
    {
        if (!_isWorkStarted ||
            _scheduleFactory == null ||
            _cancellation.IsCancellationRequested)
        {
            return;
        }

        Interlocked.Exchange(ref _refreshPending, 1);
        var gateEntered = false;
        try
        {
            if (!await _refreshGate.WaitAsync(0, _cancellation.Token))
            {
                return;
            }

            gateEntered = true;
            do
            {
                Interlocked.Exchange(ref _refreshPending, 0);
                await RefreshOnceAsync(_cancellation.Token);
            }
            while (Volatile.Read(ref _refreshPending) != 0 &&
                   !_cancellation.IsCancellationRequested);
        }
        catch (OperationCanceledException)
        {
            // 应用正在停止。
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"同步 iOS/iPadOS 课程通知时发生异常：{exception}");
        }
        finally
        {
            if (gateEntered)
            {
                _refreshGate.Release();
                if (Volatile.Read(ref _refreshPending) != 0 &&
                    !_cancellation.IsCancellationRequested)
                {
                    QueueRefresh();
                }
            }
        }
    }

    private async Task RefreshOnceAsync(CancellationToken cancellationToken)
    {
        var scheduleFactory = _scheduleFactory;
        if (scheduleFactory == null)
        {
            return;
        }

        var authorized = await _authorizationService.RequestAuthorizationIfNeededAsync();
        var requests = authorized
            ? scheduleFactory.Create()
            : Array.Empty<IosLessonNotificationRequest>();
        if (_lastAuthorizationState == authorized &&
            _lastRequests != null &&
            _lastRequests.SequenceEqual(requests))
        {
            return;
        }

        await _scheduler.SynchronizeAsync(requests, cancellationToken);

        _lastAuthorizationState = authorized;
        _lastRequests = requests.ToArray();
        if (!authorized)
        {
            Console.WriteLine("iOS/iPadOS 通知权限未授予。可在系统设置中手动启用。");
        }
    }

    private void QueueBackgroundRefresh()
    {
        if (!_isWorkStarted ||
            _cancellation.IsCancellationRequested ||
            Interlocked.CompareExchange(ref _backgroundRefreshActive, 1, 0) != 0)
        {
            return;
        }

        var lease = new BackgroundTaskLease(UIApplication.SharedApplication);
        if (!lease.TryStart("ClassIsland lesson notification refresh"))
        {
            lease.Dispose();
            Interlocked.Exchange(ref _backgroundRefreshActive, 0);
            return;
        }

        _ = RunBackgroundRefreshAsync(lease);
    }

    private async Task RunBackgroundRefreshAsync(BackgroundTaskLease lease)
    {
        try
        {
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                _cancellation.Token,
                lease.ExpirationToken);
            await RefreshInBackgroundAsync(linkedCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            // 应用已终止刷新，或系统收回后台执行时间。
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"iOS/iPadOS 退后台前同步课程通知时发生异常：{exception}");
        }
        finally
        {
            lease.Dispose();
            Interlocked.Exchange(ref _backgroundRefreshActive, 0);
        }
    }

    private async Task RefreshInBackgroundAsync(CancellationToken cancellationToken)
    {
        if (!_isWorkStarted || _scheduleFactory == null)
        {
            return;
        }

        await _refreshGate.WaitAsync(cancellationToken);
        try
        {
            Interlocked.Exchange(ref _refreshPending, 0);
            _lastRequests = null;
            await RefreshOnceAsync(cancellationToken);
        }
        finally
        {
            _refreshGate.Release();
            if (Volatile.Read(ref _refreshPending) != 0 &&
                !_cancellation.IsCancellationRequested)
            {
                QueueRefresh();
            }
        }
    }

    private void RebuildChangeSubscriptions()
    {
        DetachChangeSubscriptions();
        if (_settingsService == null ||
            _profileService == null ||
            _scheduleFactory == null)
        {
            return;
        }

        var settings = _settingsService.Settings;
        AddPropertyChangeSource(settings);
        AddPropertyChangeSource(_scheduleFactory.ProviderSettings);
        AddCollectionChangeSource(settings.NotificationProvidersEnableStates);
        AddCollectionChangeSource(settings.NotificationProvidersSettings);
        AddCollectionChangeSource(settings.NotificationProvidersNotifySettings);
        AddCollectionChangeSource(settings.NotificationChannelsNotifySettings);
        foreach (var source in settings.NotificationProvidersSettings.Values
                     .OfType<INotifyPropertyChanged>())
        {
            AddPropertyChangeSource(source);
        }
        foreach (var source in settings.NotificationProvidersNotifySettings.Values)
        {
            AddPropertyChangeSource(source);
        }
        foreach (var source in settings.NotificationChannelsNotifySettings.Values)
        {
            AddPropertyChangeSource(source);
        }

        var profile = _profileService.Profile;
        AddPropertyChangeSource(profile);
        AddCollectionChangeSource(profile.ClassPlans);
        AddCollectionChangeSource(profile.TimeLayouts);
        AddCollectionChangeSource(profile.Subjects);
        AddCollectionChangeSource(profile.OrderedSchedules);

        foreach (var classPlan in profile.ClassPlans.Values)
        {
            AddClassPlanChangeSource(classPlan);
        }
        if (_lessonsService?.CurrentClassPlan is { } currentClassPlan)
        {
            AddClassPlanChangeSource(currentClassPlan);
        }
        foreach (var timeLayout in profile.TimeLayouts.Values)
        {
            AddPropertyChangeSource(timeLayout);
            AddAttachedSettingsSources(timeLayout);
            if (_timeLayoutChangeSources.Add(timeLayout))
            {
                timeLayout.LayoutObjectChanged += TimeLayoutOnLayoutObjectChanged;
            }
            foreach (var item in timeLayout.Layouts)
            {
                AddPropertyChangeSource(item);
                AddAttachedSettingsSources(item);
            }
        }
        foreach (var subject in profile.Subjects.Values)
        {
            AddPropertyChangeSource(subject);
            AddAttachedSettingsSources(subject);
        }
    }

    private void AddClassPlanChangeSource(ClassPlan classPlan)
    {
        AddPropertyChangeSource(classPlan);
        AddPropertyChangeSource(classPlan.TimeRule);
        AddAttachedSettingsSources(classPlan);
        if (_classPlanChangeSources.Add(classPlan))
        {
            classPlan.ClassesChanged += ClassPlanOnClassesChanged;
        }
    }

    private void AddAttachedSettingsSources(AttachableSettingsObject source)
    {
        foreach (var attachedSource in source.AttachedObjects.Values
                     .OfType<INotifyPropertyChanged>())
        {
            _attachedSettingsSources.Add(attachedSource);
            AddPropertyChangeSource(attachedSource);
        }
    }

    private void AddPropertyChangeSource(INotifyPropertyChanged source)
    {
        if (_propertyChangeSources.Add(source))
        {
            source.PropertyChanged += ChangeSourceOnPropertyChanged;
        }
    }

    private void AddCollectionChangeSource(INotifyCollectionChanged source)
    {
        if (_collectionChangeSources.Add(source))
        {
            source.CollectionChanged += ChangeSourceOnCollectionChanged;
        }
    }

    private void ChangeSourceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if ((ReferenceEquals(sender, _settingsService?.Settings) &&
             e.PropertyName is nameof(ClassIsland.Models.Settings.NotificationProvidersEnableStates)
                 or nameof(ClassIsland.Models.Settings.NotificationProvidersSettings)
                 or nameof(ClassIsland.Models.Settings.NotificationProvidersNotifySettings)
                 or nameof(ClassIsland.Models.Settings.NotificationChannelsNotifySettings)) ||
            (ReferenceEquals(sender, _profileService?.Profile) &&
             e.PropertyName is nameof(Profile.ClassPlans)
                 or nameof(Profile.TimeLayouts)
                 or nameof(Profile.Subjects)
                 or nameof(Profile.OrderedSchedules)) ||
            (sender is ClassPlan && e.PropertyName == nameof(ClassPlan.TimeRule)) ||
            (sender is TimeLayout && e.PropertyName == nameof(TimeLayout.Layouts)))
        {
            RebuildChangeSubscriptions();
        }

        QueueRefreshDebounced();
    }

    private void ChangeSourceOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildChangeSubscriptions();
        QueueRefreshDebounced();
    }

    private void ClassPlanOnClassesChanged(object? sender, EventArgs e)
    {
        RebuildChangeSubscriptions();
        QueueRefreshDebounced();
    }

    private void TimeLayoutOnLayoutObjectChanged(object? sender, EventArgs e)
    {
        RebuildChangeSubscriptions();
        QueueRefreshDebounced();
    }

    private void LessonsServiceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ILessonsService.CurrentClassPlan))
        {
            return;
        }

        RebuildChangeSubscriptions();
        QueueRefreshDebounced();
    }

    private void ExactTimeServiceOnPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IExactTimeService.SyncStatusMessage))
        {
            QueueRefreshDebounced();
        }
    }

    private void SystemEventsOnTimeChanged(object? sender, EventArgs e)
    {
        _lastRequests = null;
        QueueRefreshDebounced();
    }

    private void QueueRefreshDebounced()
    {
        if (!_isWorkStarted || _cancellation.IsCancellationRequested)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (!_isWorkStarted || _cancellation.IsCancellationRequested)
            {
                return;
            }

            _refreshDebounceTimer.Stop();
            _refreshDebounceTimer.Start();
        });
    }

    private void RefreshDebounceTimerOnTick(object? sender, EventArgs e)
    {
        _refreshDebounceTimer.Stop();
        QueueRefresh();
    }

    private void AttachedSettingsScanTimerOnTick(object? sender, EventArgs e)
    {
        var currentSources = CollectAttachedSettingsSources();
        if (_attachedSettingsSources.SetEquals(currentSources))
        {
            return;
        }

        RebuildChangeSubscriptions();
        QueueRefreshDebounced();
    }

    private void RollingScheduleRefreshTimerOnTick(object? sender, EventArgs e) =>
        QueueRefresh();

    private HashSet<INotifyPropertyChanged> CollectAttachedSettingsSources()
    {
        var result = new HashSet<INotifyPropertyChanged>();
        var profile = _profileService?.Profile;
        if (profile == null)
        {
            return result;
        }

        static void AddSources(
            ISet<INotifyPropertyChanged> target,
            AttachableSettingsObject source)
        {
            foreach (var attachedSource in source.AttachedObjects.Values
                         .OfType<INotifyPropertyChanged>())
            {
                target.Add(attachedSource);
            }
        }

        foreach (var classPlan in profile.ClassPlans.Values)
        {
            AddSources(result, classPlan);
        }
        if (_lessonsService?.CurrentClassPlan is { } currentClassPlan)
        {
            AddSources(result, currentClassPlan);
        }
        foreach (var timeLayout in profile.TimeLayouts.Values)
        {
            AddSources(result, timeLayout);
            foreach (var item in timeLayout.Layouts)
            {
                AddSources(result, item);
            }
        }
        foreach (var subject in profile.Subjects.Values)
        {
            AddSources(result, subject);
        }

        return result;
    }

    private void DetachChangeSubscriptions()
    {
        foreach (var source in _propertyChangeSources)
        {
            source.PropertyChanged -= ChangeSourceOnPropertyChanged;
        }
        _propertyChangeSources.Clear();
        _attachedSettingsSources.Clear();

        foreach (var source in _collectionChangeSources)
        {
            source.CollectionChanged -= ChangeSourceOnCollectionChanged;
        }
        _collectionChangeSources.Clear();

        foreach (var classPlan in _classPlanChangeSources)
        {
            classPlan.ClassesChanged -= ClassPlanOnClassesChanged;
        }
        _classPlanChangeSources.Clear();

        foreach (var timeLayout in _timeLayoutChangeSources)
        {
            timeLayout.LayoutObjectChanged -= TimeLayoutOnLayoutObjectChanged;
        }
        _timeLayoutChangeSources.Clear();
    }

    public void Dispose()
    {
        if (!_isStarted)
        {
            return;
        }

        AppBase.Current.AppStarted -= OnAppStarted;
        _cancellation.Cancel();
        _refreshDebounceTimer.Stop();
        _refreshDebounceTimer.Tick -= RefreshDebounceTimerOnTick;
        _attachedSettingsScanTimer.Stop();
        _attachedSettingsScanTimer.Tick -= AttachedSettingsScanTimerOnTick;
        _rollingScheduleRefreshTimer.Stop();
        _rollingScheduleRefreshTimer.Tick -= RollingScheduleRefreshTimerOnTick;
        if (_lessonsService != null)
        {
            _lessonsService.PropertyChanged -= LessonsServiceOnPropertyChanged;
        }
        if (_exactTimeService != null)
        {
            _exactTimeService.PropertyChanged -= ExactTimeServiceOnPropertyChanged;
        }
        PlatformServices.SystemEventsService.TimeChanged -= SystemEventsOnTimeChanged;
        _notificationHostService?.UnregisterNotificationConsumer(_queueConsumer);
        DetachChangeSubscriptions();
        DisposeObserver(ref _foregroundObserver);
        DisposeObserver(ref _backgroundObserver);
        _lessonsService = null;
        _profileService = null;
        _exactTimeService = null;
        _settingsService = null;
        _notificationHostService = null;
        _scheduleFactory = null;
        _isStarted = false;
        _isWorkStarted = false;
    }

    private static void DisposeObserver(ref NSObject? observer)
    {
        if (observer == null)
        {
            return;
        }

        NSNotificationCenter.DefaultCenter.RemoveObserver(observer);
        observer.Dispose();
        observer = null;
    }

    private sealed class BackgroundTaskLease(UIApplication application) : IDisposable
    {
        private readonly CancellationTokenSource _expirationCancellation = new();
        private readonly object _syncRoot = new();
        private nint _identifier = UIApplication.BackgroundTaskInvalid;
        private bool _identifierAssigned;
        private bool _ended;

        public CancellationToken ExpirationToken => _expirationCancellation.Token;

        public bool TryStart(string name)
        {
            var identifier = application.BeginBackgroundTask(name, OnExpired);
            bool endAfterAssignment;
            lock (_syncRoot)
            {
                _identifier = identifier;
                _identifierAssigned = true;
                endAfterAssignment = _ended &&
                                     identifier != UIApplication.BackgroundTaskInvalid;
            }

            if (endAfterAssignment)
            {
                application.EndBackgroundTask(identifier);
            }

            return identifier != UIApplication.BackgroundTaskInvalid &&
                   !endAfterAssignment;
        }

        private void OnExpired()
        {
            try
            {
                _expirationCancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // 正常结束与系统到期回调并发时，租约可能已经释放。
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"取消 iOS 后台通知刷新时发生异常：{exception}");
            }
            finally
            {
                End();
            }
        }

        private void End()
        {
            var identifier = UIApplication.BackgroundTaskInvalid;
            lock (_syncRoot)
            {
                if (_ended)
                {
                    return;
                }

                _ended = true;
                if (_identifierAssigned)
                {
                    identifier = _identifier;
                }
            }

            if (identifier != UIApplication.BackgroundTaskInvalid)
            {
                application.EndBackgroundTask(identifier);
            }
        }

        public void Dispose()
        {
            End();
            _expirationCancellation.Dispose();
        }
    }
}
