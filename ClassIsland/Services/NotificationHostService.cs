using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Notification;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Models;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

/// <summary>
/// 提醒主机服务。
/// </summary>
public class NotificationHostService(SettingsService settingsService, ILogger<NotificationHostService> logger)
    : IHostedService, INotifyPropertyChanged, INotificationHostService
{
    private SettingsService SettingsService { get; } = settingsService;
    private ILogger<NotificationHostService> Logger { get; } = logger;
    private Settings Settings => SettingsService.Settings;

    public PriorityQueue<NotificationRequest, int> RequestQueue { get; } = new();

    public ObservableCollection<NotificationProviderRegisterInfo> NotificationProviders { get; } = new();

    #region Events

    public event EventHandler? UpdateTimerTick;
    public void OnUpdateTimerTick(object sender, EventArgs args) => UpdateTimerTick?.Invoke(sender, args);
    
    public event EventHandler? OnClass;
    public void OnOnClass(object sender, EventArgs args) => OnClass?.Invoke(sender, args);

    public event EventHandler? OnBreakingTime;
    public void OnOnBreakingTime(object sender, EventArgs args) => OnBreakingTime?.Invoke(sender, args);

    public event EventHandler? CurrentStateChanged;
    public void OnCurrentStateChanged(object sender, EventArgs args) => CurrentStateChanged?.Invoke(sender, args);

    #endregion
    private TimeSpan _onClassDeltaTime = TimeSpan.Zero;
    private TimeSpan _onBreakingTimeDeltaTime = TimeSpan.Zero;
    private Subject _nextClassSubject = new Subject();
    private TimeLayoutItem _nextClassTimeLayoutItem = new();
    private TimeLayoutItem _nextBreakingTimeLayoutItem = new();
    private bool _isClassPlanLoaded = false;
    private bool _isClassConfirmed = false;
    private TimeState _currentState = TimeState.None;

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

    public bool IsClassConfirmed
    {
        get => _isClassConfirmed;
        set => SetField(ref _isClassConfirmed, value);
    }

    public TimeState CurrentState
    {
        get => _currentState;
        set => SetField(ref _currentState, value);
    }

    public NotificationRequest? CurrentRequest { get; set; }

    public NotificationRequest GetRequest()
    {
        CurrentRequest = RequestQueue.Dequeue();
        CurrentRequest.CancellationTokenSource.Token.Register(() =>
        {
            while (RequestQueue.Count > 0)
            {
                var r = RequestQueue.Dequeue();
                r.CompletedTokenSource.Cancel();
            }
        });
        return CurrentRequest;
    }

    /// <summary>
    /// 注册提醒服务。
    /// </summary>
    /// <param name="provider">要注册的服务实例。</param>
    /// <example>
    /// <code>
    /// NotificationHostService.RegisterNotificationProvider(this);
    /// </code>
    /// </example>
    public void RegisterNotificationProvider(INotificationProvider provider)
    {
        Logger.LogInformation("注册提醒提供方：{}（{}）", provider.ProviderGuid, provider.Name);
        if (!Settings.NotificationProvidersPriority.Contains(provider.ProviderGuid.ToString()))
        {
            Settings.NotificationProvidersPriority.Add(provider.ProviderGuid.ToString());
        }
        if (!Settings.NotificationProvidersSettings.ContainsKey(provider.ProviderGuid.ToString()))
        {
            Settings.NotificationProvidersSettings.Add(provider.ProviderGuid.ToString(), null);
        }
        if (!Settings.NotificationProvidersEnableStates.ContainsKey(provider.ProviderGuid.ToString()))
        {
            Settings.NotificationProvidersEnableStates.Add(provider.ProviderGuid.ToString(), true);
        }
        if (!Settings.NotificationProvidersNotifySettings.ContainsKey(provider.ProviderGuid.ToString()))
        {
            Settings.NotificationProvidersNotifySettings.Add(provider.ProviderGuid.ToString(), new());
        }

        NotificationProviders.Add(new NotificationProviderRegisterInfo(provider)
        {
            ProviderSettings = Settings.NotificationProvidersNotifySettings[provider.ProviderGuid.ToString()]
        });
    }

    public void ShowNotification(NotificationRequest request)
    {
        var trace = new StackTrace();
        Logger.LogDebug("准备显示提醒\n{}", trace);
        foreach (var i in trace.GetFrames())
        {
            var type = i.GetMethod()?.DeclaringType;
            if (type?.IsAssignableTo(typeof(INotificationProvider)) != true)
                continue;
            var provider = (from p in NotificationProviders where p.ProviderInstance.GetType() == type select p).FirstOrDefault();
            if (provider == null)
                continue;
            Logger.LogInformation("请求来源：{}", provider.ProviderGuid);
            ShowNotification(request, provider.ProviderGuid);
            return;
        }

        throw new ArgumentException("此方法只能由 INotificationProvider 调用。");
    }

    private void ShowNotification(NotificationRequest request, Guid providerGuid)
    {
        request.NotificationSourceGuid = providerGuid;
        request.NotificationSource = (from i in NotificationProviders where i.ProviderGuid == providerGuid select i)
            .FirstOrDefault();
        request.ProviderSettings = request.NotificationSource?.ProviderSettings ?? request.ProviderSettings;
        RequestQueue.Enqueue(request, request.IsPriorityOverride ? request.PriorityOverride : Settings.NotificationProvidersPriority.IndexOf(providerGuid.ToString()));
    }

    public async Task ShowNotificationAsync(NotificationRequest request)
    {
        ShowNotification(request);
        await Task.Run(() =>
        {
            request.CompletedTokenSource.Token.WaitHandle.WaitOne();
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

    /// <summary>
    /// 获取提醒服务实例。
    /// </summary>
    /// <typeparam name="T">提醒服务类型</typeparam>
    /// <param name="id">提醒服务id</param>
    /// <returns>对应提醒服务实例。若不存在，则返回null。</returns>
    public T? GetNotificationProviderSettings<T>(Guid id)
    {
        Logger.LogInformation("获取提醒提供方设置：{}", id);
        var o = Settings.NotificationProvidersSettings[id.ToString()];
        if (o is JsonElement)
        {
            var o1 = (JsonElement)o;
            return o1.Deserialize<T>();
        }
        return (T?)Settings.NotificationProvidersSettings[id.ToString()];
    }

    public void WriteNotificationProviderSettings<T>(Guid id, T settings)
    {
        Logger.LogInformation("写入提醒提供方设置：{}", id);
        Settings.NotificationProvidersSettings[id.ToString()] = settings;
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