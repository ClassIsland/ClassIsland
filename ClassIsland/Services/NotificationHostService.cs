using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Interfaces;
using ClassIsland.Models;
using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services;

public class NotificationHostService : IHostedService, INotifyPropertyChanged
{
    private SettingsService SettingsService { get; }
    private Settings Settings => SettingsService.Settings;

    public Queue<NotificationRequest> RequestQueue { get; } = new();

    public ObservableCollection<INotificationProvider> NotificationProviders { get; } = new();

    public NotificationHostService(SettingsService settingsService)
    {
        SettingsService = settingsService;
    }

    #region Events

    public EventHandler? UpdateTimerTick;
    public void OnUpdateTimerTick(object sender, EventArgs args) => UpdateTimerTick?.Invoke(sender, args);
    
    public EventHandler? OnClass;
    public void OnOnClass(object sender, EventArgs args) => OnClass?.Invoke(sender, args);

    
    public void OnOnBreakingTime(object sender, EventArgs args) => OnBreakingTime?.Invoke(sender, args);

    #endregion
    public EventHandler? OnBreakingTime;
    private TimeSpan _onClassDeltaTime = TimeSpan.Zero;
    private TimeSpan _onBreakingTimeDeltaTime = TimeSpan.Zero;
    private Subject _nextClassSubject = new Subject();
    private TimeLayoutItem _nextClassTimeLayoutItem = new();
    private TimeLayoutItem _nextBreakingTimeLayoutItem = new();
    private bool _isClassPlanLoaded = false;

    public TimeSpan OnClassDeltaTime
    {
        get => _onClassDeltaTime;
        set => SetField(ref _onClassDeltaTime, value);
    }

    public TimeSpan OnBreakingTimeDeltaTime
    {
        get => _onBreakingTimeDeltaTime;
        set => SetField(ref _onBreakingTimeDeltaTime, value);
    }

    public Subject NextClassSubject
    {
        get => _nextClassSubject;
        set => SetField(ref _nextClassSubject, value);
    }

    public TimeLayoutItem NextClassTimeLayoutItem
    {
        get => _nextClassTimeLayoutItem;
        set => SetField(ref _nextClassTimeLayoutItem, value);
    }

    public TimeLayoutItem NextBreakingTimeLayoutItem
    {
        get => _nextBreakingTimeLayoutItem;
        set => SetField(ref _nextBreakingTimeLayoutItem, value);
    }

    public bool IsClassPlanLoaded
    {
        get => _isClassPlanLoaded;
        set => SetField(ref _isClassPlanLoaded, value);
    }

    public void RegisterNotificationProvider(INotificationProvider provider)
    {
        if (!Settings.NotificationProvidersPriority.Contains(provider.ProviderGuid.ToString()))
        {
            Settings.NotificationProvidersPriority.Add(provider.ProviderGuid.ToString());
            Settings.NotificationProvidersEnableStates.Add(provider.ProviderGuid.ToString(), true);
            Settings.NotificationProvidersSettings.Add(provider.ProviderGuid.ToString(), null);
        }
        NotificationProviders.Add(provider);
    }

    public async Task ShowNotificationAsync(NotificationRequest request)
    {
        RequestQueue.Enqueue(request);
        await Task.Run(() =>
        {
            while (RequestQueue.Contains(request))
            {

            }
        });

    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return new Task(()=>{});
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return new Task(()=>{});
    }

    public T? GetNotificationProviderSettings<T>(Guid id)
    {
        return (T?)Settings.NotificationProvidersSettings[id.ToString()];
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}