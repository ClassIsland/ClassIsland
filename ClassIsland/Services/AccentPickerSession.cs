using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

internal sealed class AccentPickerSession : ObservableObject
{
    private static readonly AccentPickResult Unavailable = new(AccentPickStatus.Unavailable);
    private static readonly AccentPickResult Failed = new(AccentPickStatus.Failed);

    private readonly SettingsService _settingsService;
    private readonly ILogger<AccentPickerSession> _logger;
    private readonly Func<AccentCaptureTarget, CancellationToken, Task<AccentPickResult>> _pickAsync;
    private readonly bool _supportsWallpaper;
    private readonly bool _supportsScreen;
    private readonly DispatcherTimer _timer = new();
    private readonly DispatcherTimer _wallpaperDebounce = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly CancellationTokenSource _lifetime = new();
    private Settings? _settings;
    private Task<AccentPickResult>? _refresh;
    private long _revision;
    private long _refreshRevision;
    private AccentPickResult? _lastResult;

    public AccentPickerSession(SettingsService settingsService, ILogger<AccentPickerSession> logger,
        bool supportsWallpaper, bool supportsScreen, Func<AccentCaptureTarget, CancellationToken, Task<AccentPickResult>> pickAsync)
    {
        _settingsService = settingsService;
        _logger = logger;
        _supportsWallpaper = supportsWallpaper;
        _supportsScreen = supportsScreen;
        _pickAsync = pickAsync;
        _timer.Tick += (_, _) =>
        {
            if (_refresh == null)
                _ = RefreshAsync();
        };
        _wallpaperDebounce.Tick += (_, _) =>
        {
            _wallpaperDebounce.Stop();
            if (_settings?.ColorSource == 1)
                _ = RefreshAsync();
        };
        AppBase.Current.AppStarted += OnAppStarted;
        AppBase.Current.AppStopping += OnAppStopping;
    }

    public bool IsWorking => _refresh != null;

    public AccentPickResult? LastResult
    {
        get => _lastResult;
        private set => SetProperty(ref _lastResult, value);
    }

    public Task<AccentPickResult> RefreshAsync(CancellationToken cancellationToken = default) =>
        Dispatcher.UIThread.CheckAccess()
            ? RefreshOnUiThreadAsync(cancellationToken)
            : Dispatcher.UIThread.InvokeAsync(() => RefreshOnUiThreadAsync(cancellationToken));

    public void OnWallpaperChanged() => Dispatcher.UIThread.Post(() =>
    {
        if (_settings?.ColorSource != 1)
            return;
        _wallpaperDebounce.Stop();
        _wallpaperDebounce.Start();
    });

    private void OnAppStarted(object? sender, EventArgs e)
    {
        AppBase.Current.AppStarted -= OnAppStarted;
        _settingsService.PropertyChanged += OnSettingsReplaced;
        ObserveSettings();
    }

    private void OnAppStopping(object? sender, EventArgs e)
    {
        AppBase.Current.AppStarted -= OnAppStarted;
        AppBase.Current.AppStopping -= OnAppStopping;
        _settingsService.PropertyChanged -= OnSettingsReplaced;
        if (_settings != null)
            _settings.PropertyChanged -= OnSettingsChanged;
        _settings = null;
        _timer.Stop();
        _wallpaperDebounce.Stop();
        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    private void OnSettingsReplaced(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsService.Settings))
            RunOnUiThread(ObserveSettings);
    }

    private void ObserveSettings()
    {
        if (_lifetime.IsCancellationRequested)
            return;
        if (_settings != null)
            _settings.PropertyChanged -= OnSettingsChanged;
        _settings = _settingsService.Settings;
        _settings.PropertyChanged += OnSettingsChanged;
        InvalidateTarget();
        UpdateTimer();
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e) => RunOnUiThread(() =>
    {
        if (_settings == null || !ReferenceEquals(_settings, sender))
            return;

        switch (e.PropertyName)
        {
            case nameof(Settings.ColorSource):
                UpdateTimer();
                InvalidateTarget();
                break;
            case nameof(Settings.WallpaperClassName) or nameof(Settings.IsFallbackModeEnabled)
                when _settings.ColorSource == 1 && _supportsWallpaper:
            case nameof(Settings.WindowDockingMonitorIndex) when _settings.ColorSource == 3:
                InvalidateTarget();
                break;
            case nameof(Settings.IsWallpaperAutoUpdateEnabled) or nameof(Settings.WallpaperAutoUpdateIntervalSeconds):
                UpdateTimer();
                break;
        }
    });

    private void InvalidateTarget()
    {
        _revision++;
        LastResult = null;
        _wallpaperDebounce.Stop();
        _ = RefreshAsync();
    }

    private void UpdateTimer()
    {
        _timer.Stop();
        if (_settings == null || !CanPick(_settings.ColorSource) ||
            (_settings.ColorSource == 1 && !_settings.IsWallpaperAutoUpdateEnabled))
            return;

        var seconds = _settings.WallpaperAutoUpdateIntervalSeconds;
        _timer.Interval = TimeSpan.FromSeconds(seconds <= 0 ? 60 : Math.Min(seconds, 36000));
        _timer.Start();
    }

    private bool CanPick(int source) => source switch
    {
        1 => _supportsWallpaper,
        3 => _supportsScreen,
        _ => false
    };

    private async Task<AccentPickResult> RefreshOnUiThreadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var revision = _revision;
        while (_settings is { } settings && CanPick(settings.ColorSource) && revision == _revision)
        {
            if (_refresh == null)
            {
                _refreshRevision = revision;
                _refresh = PickAndSaveAsync(settings, AccentCaptureTarget.FromSettings(settings), revision, _lifetime.Token);
                OnPropertyChanged(nameof(IsWorking));
            }

            var refreshRevision = _refreshRevision;
            var result = await _refresh.WaitAsync(cancellationToken);
            if (refreshRevision == revision)
                return revision == _revision ? result : Unavailable;
        }
        return Unavailable;
    }

    private async Task<AccentPickResult> PickAndSaveAsync(Settings settings, AccentCaptureTarget target, long revision,
        CancellationToken token)
    {
        // Publish the shared task before any capture or completion can run.
        await Task.Yield();
        try
        {
            if (revision != _revision || token.IsCancellationRequested)
                return Unavailable;

            var result = await _pickAsync(target, token);
            if (revision != _revision || token.IsCancellationRequested || !ReferenceEquals(settings, _settingsService.Settings) ||
                target != AccentCaptureTarget.FromSettings(settings))
                return Unavailable;

            if (result.Status == AccentPickStatus.Success && result.Color is { } color)
            {
                _settingsService.SaveExtractedAccent(color);
                LastResult = result;
            }
            return result;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return Unavailable;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "无法刷新主题色。");
            return Failed;
        }
        finally
        {
            _refresh = null;
            OnPropertyChanged(nameof(IsWorking));
        }
    }

    private static void RunOnUiThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.Post(action);
    }
}

internal readonly record struct AccentCaptureTarget(int Source, string WindowClassName, bool CompatibilityMode, int MonitorIndex)
{
    public static AccentCaptureTarget FromSettings(Settings settings) => settings.ColorSource == 3
        ? new(3, "", false, settings.WindowDockingMonitorIndex)
        : new(settings.ColorSource, settings.WallpaperClassName ?? "", settings.IsFallbackModeEnabled, -1);
}
