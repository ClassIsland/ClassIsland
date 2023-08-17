using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Interfaces;
using ClassIsland.Models;
using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services;

public class NotificationHostService : IHostedService
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

    
    public EventHandler? OnBreakingTime;
    public void OnOnBreakingTime(object sender, EventArgs args) => OnBreakingTime?.Invoke(sender, args);

    #endregion

    public TimeSpan OnClassDeltaTime { get; set; } = TimeSpan.Zero;
    public TimeSpan OnBreakingTimeDeltaTime { get; set; } = TimeSpan.Zero;
    public Subject NextClassSubject { get; set; } = new Subject();
    public TimeLayoutItem NextTimeLayoutItem { get; set; } = new TimeLayoutItem();

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
}