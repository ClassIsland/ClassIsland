using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models;
using ClassIsland.Models.SystemIntegration;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Services;
using ClassIsland.Services.SystemIntegration;
using ClassIsland.Shared;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;

namespace ClassIsland.iOS.Services.SystemIntegration;

/// <summary>
/// 在应用运行时更新系统快照；挂起后由 WidgetKit 时间线继续切换课程。
/// </summary>
internal sealed class IosSystemIntegrationCoordinator : IDisposable
{
    private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromMinutes(1) };
    private readonly DispatcherTimer _debounceTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private ILessonsService? _lessons;
    private IExactTimeService? _exactTime;
    private SettingsService? _settings;
    private Settings? _observedSettings;
    private SystemScheduleSnapshotFactory? _factory;
    private NSObject? _foregroundObserver;
    private NSObject? _backgroundObserver;
    private bool _disposed;
    private bool _hasLoggedFailure;
    private bool _shouldReloadWidgets = true;
    private ILogger<IosSystemIntegrationCoordinator>? _logger;

    public void Start()
    {
        _logger = IAppHost.GetService<ILogger<IosSystemIntegrationCoordinator>>();
        _lessons = IAppHost.GetService<ILessonsService>();
        _settings = IAppHost.GetService<SettingsService>();
        _exactTime = IAppHost.GetService<IExactTimeService>();
        _factory = new SystemScheduleSnapshotFactory(_lessons,
            IAppHost.GetService<IProfileService>(), _exactTime);
        _lessons.ScheduleItemsInvalidated += OnScheduleInvalidated;
        _lessons.PropertyChanged += OnLessonsChanged;
        _settings.PropertyChanged += OnSettingsReplaced;
        _exactTime.PropertyChanged += OnExactTimeChanged;
        ObserveSettings();
        PlatformServices.SystemEventsService.TimeChanged += OnTimeChanged;
        _foregroundObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            UIApplication.WillEnterForegroundNotification, _ => QueueRefresh(reloadWidgets: true));
        _backgroundObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            UIApplication.DidEnterBackgroundNotification, _ => FlushBeforeSuspension());
        _refreshTimer.Tick += OnRefreshTimer;
        _debounceTimer.Tick += OnDebounceTimer;
        _refreshTimer.Start();
        Refresh();
    }

    private void OnScheduleInvalidated(object? sender, Predicate<DateOnly> args) => QueueRefresh();

    private void OnTimeChanged(object? sender, EventArgs args) => QueueRefresh();

    private void OnExactTimeChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(IExactTimeService.SyncStatusMessage)) QueueRefresh();
    }

    private void FlushBeforeSuspension()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(FlushBeforeSuspension, DispatcherPriority.Send);
            return;
        }
        // 进入后台后不再等待防抖定时器，避免系统先挂起应用而留下旧快照。
        _debounceTimer.Stop();
        Refresh();
    }

    private void OnRefreshTimer(object? sender, EventArgs args) => Refresh();

    private void OnDebounceTimer(object? sender, EventArgs args)
    {
        _debounceTimer.Stop();
        Refresh();
    }

    private void OnLessonsChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(ILessonsService.CurrentClassPlan) or
            nameof(ILessonsService.IsClassPlanEnabled) or nameof(ILessonsService.IsClassPlanLoaded))
        {
            QueueRefresh();
        }
    }

    private void OnSettingsReplaced(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(SettingsService.Settings))
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_disposed) return;
                ObserveSettings();
                QueueRefresh();
            });
        }
    }

    private void ObserveSettings()
    {
        if (_observedSettings != null) _observedSettings.PropertyChanged -= OnSettingsChanged;
        _observedSettings = _settings!.Settings;
        _observedSettings.PropertyChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(Settings.IsNotificationEnabled)) QueueRefresh();
    }

    private void QueueRefresh(bool reloadWidgets = false) => Dispatcher.UIThread.Post(() =>
    {
        if (_disposed) return;
        // 小组件可能缓存了读取失败时的时间线，快照未变时原生层不会主动刷新。
        _shouldReloadWidgets |= reloadWidgets;
        if (!_debounceTimer.IsEnabled) _debounceTimer.Start();
    });

    private void Refresh()
    {
        if (_disposed || _factory == null || _settings == null) return;
        try
        {
            var snapshot = _factory.Create(_settings.Settings.IsNotificationEnabled, DateTimeOffset.Now);
            IosSystemIntegrationBridge.Publish(JsonSerializer.Serialize(
                snapshot, SystemScheduleJsonContext.Default.SystemScheduleSnapshot));
            _hasLoggedFailure = false;
        }
        catch (Exception exception)
        {
            if (!_hasLoggedFailure)
            {
                _logger?.LogError(exception, "无法同步系统课表，小组件和系统搜索暂时无法更新。");
                _hasLoggedFailure = true;
            }
        }

        // 同步失败时也刷新，让小组件按当前签名状态显示对应提示。
        if (_shouldReloadWidgets)
        {
            _shouldReloadWidgets = false;
            try
            {
                IosSystemIntegrationBridge.ReloadWidgets();
            }
            catch (Exception exception)
            {
                _logger?.LogWarning(exception, "无法刷新小组件。");
            }
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _refreshTimer.Stop();
        _debounceTimer.Stop();
        _foregroundObserver?.Dispose();
        _backgroundObserver?.Dispose();
        if (_lessons != null)
        {
            _lessons.ScheduleItemsInvalidated -= OnScheduleInvalidated;
            _lessons.PropertyChanged -= OnLessonsChanged;
        }
        if (_settings != null) _settings.PropertyChanged -= OnSettingsReplaced;
        if (_exactTime != null) _exactTime.PropertyChanged -= OnExactTimeChanged;
        if (_observedSettings != null) _observedSettings.PropertyChanged -= OnSettingsChanged;
        PlatformServices.SystemEventsService.TimeChanged -= OnTimeChanged;
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSerializable(typeof(SystemScheduleSnapshot))]
internal partial class SystemScheduleJsonContext : JsonSerializerContext;
