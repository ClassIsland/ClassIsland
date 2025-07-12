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
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Services.Registry;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Models;
using ClassIsland.Models.Notification;
using ClassIsland.Shared.Models.Notification;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationRequest = ClassIsland.Core.Models.Notification.NotificationRequest;

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

    public PriorityQueue<NotificationRequest, NotificationPriority> RequestQueue { get; } = new();

    private int _queueIndex = 0;
    private bool _isNotificationsPlaying = false;

    public ObservableCollection<NotificationProviderRegisterInfo> NotificationProviders { get; } = new();

    private List<NotificationConsumerRegisterInfo> RegisteredConsumers { get; } = [];

    private List<NotificationRequest> PlayingNotifications { get; } = [];

    public NotificationRequest? CurrentRequest { get; set; }

    public NotificationRequest GetRequest()
    {
        CurrentRequest = RequestQueue.Dequeue();
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

        if (provider is not NotificationProviderBase providerBase)
        {
            return;
        }

        var providerInfo = NotificationProviderRegistryService.RegisteredProviders.First(x => x.Guid == provider.ProviderGuid);
        foreach (var channelInfo in providerInfo.RegisteredChannels)
        {
            providerBase.Channels[channelInfo.Guid] = new NotificationChannel(providerBase, providerInfo, channelInfo);
        }
    }

    private void UpdateNotificationPlayingState()
    {
        IsNotificationsPlaying = PlayingNotifications.Count > 0;
    }

    private void FinishNotificationPlaying(NotificationRequest request)
    {
        Logger.LogTrace("提醒 #{} 已播放完成", request.GetHashCode());
        PlayingNotifications.Remove(request);
        UpdateNotificationPlayingState();
    }

    public void ShowNotification(NotificationRequest request, Guid providerGuid, Guid channelGuid, bool pushNotifications)
    {
        request.NotificationSourceGuid = providerGuid;
        request.NotificationSource = (from i in NotificationProviders where i.ProviderGuid == providerGuid select i)
            .FirstOrDefault();
        request.ProviderSettings = request.NotificationSource?.ProviderSettings ?? request.ProviderSettings;
        if (_queueIndex +1 >= int.MaxValue)
        {
            _queueIndex = 0;
        }

        if (channelGuid != Guid.Empty && request.ChannelId == Guid.Empty)
        {
            request.ChannelId = channelGuid;
        }

        channelGuid = request.ChannelId;

        var channel =
            request.NotificationSource?.NotificationChannels.FirstOrDefault(x => x.ProviderGuid == channelGuid);
        request.ChannelSettings = channel?.ProviderSettings;
        UpdateNotificationPlayingState();
        request.CompletedToken.Register(() => FinishNotificationPlaying(request));
        if (pushNotifications && PushNotificationRequests([request]))
        {
            return;
        }
        // 如果没有消费者接收推送的提醒，则会加入提醒队列。
        RequestQueue.Enqueue(request, new NotificationPriority(Settings.NotificationProvidersPriority.IndexOf(providerGuid.ToString()), _queueIndex++, request.IsPriorityOverride) );

    }

    public async Task ShowNotificationAsync(NotificationRequest request, Guid providerGuid, Guid channelGuid)
    {
        ShowNotification(request, providerGuid, channelGuid, true);
        await Task.Run(() =>
        {
            request.CompletedTokenSource.Token.WaitHandle.WaitOne();
        });
    }

    public void ShowChainedNotifications(NotificationRequest[] requests, Guid providerGuid, Guid channelGuid)
    {
        if (requests.Length <= 0)
        {
            return;
        }

        var rootCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(requests.Select(x => x.CancellationTokenSource.Token).ToArray());
        var rootCompletedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(requests.Select(x => x.CompletedTokenSource.Token).ToArray());
        rootCancellationTokenSource.Token.Register(() =>
        {
            foreach (var request in requests.Where(x => !x.CancellationToken.IsCancellationRequested))
            {
                request.CancellationTokenSource.Cancel();
            }
        });
        NotificationRequest? prevRequest = null;
        foreach (var request in requests)
        {
            request.RootCancellationTokenSource = rootCancellationTokenSource;
            request.RootCompletedTokenSource = rootCompletedTokenSource;
            if (prevRequest != null)
            {
                prevRequest.ChainedNextRequest = request;
            }
            prevRequest = request;
            request.CompletedToken.Register(() => FinishNotificationPlaying(request));
        }

        if (PushNotificationRequests(requests.ToList()))
        {
            return;
        }
        // 如果没有消费者接收推送的提醒，则会加入提醒队列。
        foreach (var request in requests)
        {
            ShowNotification(request, providerGuid, channelGuid, false);
        }
    }

    public async Task ShowChainedNotificationsAsync(NotificationRequest[] requests, Guid providerGuid, Guid channelGuid)
    {
        if (requests.Length <= 0)
        {
            return;
        }
        ShowChainedNotifications(requests, providerGuid, channelGuid);
        await Task.Run(() =>
        {
            requests.Last().CompletedTokenSource.Token.WaitHandle.WaitOne();
        });
    }

    public void RegisterNotificationChannel(NotificationChannel channel)
    {
        Logger.LogInformation("注册提醒渠道：{}（{}）", channel.ChannelInfo.Guid, channel.ChannelInfo.Name);
        if (!Settings.NotificationChannelsNotifySettings.ContainsKey(channel.ChannelInfo.Guid.ToString()))
        {
            Settings.NotificationChannelsNotifySettings.Add(channel.ChannelInfo.Guid.ToString(), new());
        }
        NotificationProviders.FirstOrDefault(x => x.ProviderGuid == channel.ChannelInfo.AssociatedProviderGuid)?.NotificationChannels.Add(new NotificationChannelRegisterInfo(channel)
        {
            ProviderSettings = Settings.NotificationChannelsNotifySettings[channel.ChannelInfo.Guid.ToString()]
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
    public T GetNotificationProviderSettings<T>(Guid id) where T : class
    {
        Logger.LogInformation("获取提醒提供方设置：{}", id);
        var o = Settings.NotificationProvidersSettings[id.ToString()];
        var settings = o switch
        {
            JsonElement json => json.Deserialize<T>() ?? Activator.CreateInstance<T>(),
            T s => s,
            _ => Activator.CreateInstance<T>()
        };
        Settings.NotificationProvidersSettings[id.ToString()] = settings;
        return settings;
    }

    public void WriteNotificationProviderSettings<T>(Guid id, T settings)
    {
        Logger.LogInformation("写入提醒提供方设置：{}", id);
        Settings.NotificationProvidersSettings[id.ToString()] = settings;
    }

    public void CancelAllNotifications()
    {
        while (RequestQueue.Count > 0)
        {
            var r = RequestQueue.Dequeue();
            r.CompletedTokenSource.Cancel();
        }
        foreach (var request in PlayingNotifications.ToList())
        {
            // PlayingNotifications.Remove(request);
            request.CancellationTokenSource.Cancel();
            request.CompletedTokenSource.Cancel();
        }
    }

    private bool PushNotificationRequests(List<NotificationRequest> requests)
    {
        Logger.LogTrace("开始推送提醒 ({})", requests.Count);

        var consumer = RegisteredConsumers
            .FirstOrDefault(x => x.Consumer.AcceptsNotificationRequests && x.Consumer.QueuedNotificationCount <= 0);
        if (consumer != null)
        {
            Logger.LogTrace("将推送的提醒消费者：{}(#{})", consumer.Consumer, consumer.Consumer.GetHashCode());
            foreach (var request in requests)
            {
                PlayingNotifications.Add(request);
            }
            UpdateNotificationPlayingState();
            consumer.Consumer.ReceiveNotifications(requests);
            return true;
        }

        Logger.LogTrace("找不到接受提醒的提醒消费者");
        return false;
    }

    private List<NotificationRequest> PopRequests()
    {
        if (RequestQueue.Count <= 0)
        {
            return [];
        }
        
        var head = RequestQueue.Dequeue();
        List<NotificationRequest> requests = [];

        while (head != null)
        {
            requests.Add(head);
            PlayingNotifications.Add(head);
            head = head.ChainedNextRequest;
        }
        
        UpdateNotificationPlayingState();
        return requests;
    }

    private void PopRequestsToConsumers()
    {
        PushNotificationRequests(PopRequests());
    }

    public void RegisterNotificationConsumer(INotificationConsumer consumer, int priority)
    {
        var registerInfo = new NotificationConsumerRegisterInfo(consumer, priority);

        for (var i = 0; i < RegisteredConsumers.Count; i++)
        {
            if (RegisteredConsumers[i].Priority <= registerInfo.Priority) 
                continue;
            RegisteredConsumers.Insert(i, registerInfo);
            PopRequestsToConsumers();
            return;
        }
        
        RegisteredConsumers.Add(registerInfo);  // 当列表中什么都没有或者插入项的优先级比列表里所有元素都大时，插入到最后一项。
        if (consumer.AcceptsNotificationRequests && consumer.QueuedNotificationCount <= 0)
        {
            consumer.ReceiveNotifications(PopRequests());
        }
    }

    public void UnregisterNotificationConsumer(INotificationConsumer consumer)
    {
        var registerInfo = RegisteredConsumers.FirstOrDefault(x => x.Consumer == consumer);
        if (registerInfo == null)
        {
            return;
        }

        RegisteredConsumers.Remove(registerInfo);
    }

    public IList<NotificationRequest> PullNotificationRequests()
    {
        return PopRequests();
    }

    public bool IsNotificationsPlaying
    {
        get => _isNotificationsPlaying;
        set
        {
            if (value == _isNotificationsPlaying) return;
            _isNotificationsPlaying = value;
            OnPropertyChanged();
        }
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