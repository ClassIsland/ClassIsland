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
using Avalonia.Threading;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Enums.Notification;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Core.Services.Registry;
using ClassIsland.Shared;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Shared.Models.Notification;
using ClassIsland.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationRequest = ClassIsland.Core.Models.Notification.NotificationRequest;

namespace ClassIsland.Services;

/// <summary>
/// 提醒主机服务。
/// </summary>
public class NotificationHostService(SettingsService settingsService, ILogger<NotificationHostService> logger, INotificationWorkerService notificationWorkerService)
    : IHostedService, INotifyPropertyChanged, INotificationHostService
{
    private SettingsService SettingsService { get; } = settingsService;
    private ILogger<NotificationHostService> Logger { get; } = logger;
    private INotificationWorkerService NotificationWorkerService { get; } = notificationWorkerService;
    private Settings Settings => SettingsService.Settings;

    public PriorityQueue<NotificationGroup, NotificationPriority> RequestQueue { get; } = new();

    private readonly object _syncLock = new();

    private int _queueIndex = 0;
    private bool _isNotificationsPlaying = false;

    public ObservableCollection<NotificationProviderRegisterInfo> NotificationProviders { get; } = new();

    private List<NotificationConsumerRegisterInfo> RegisteredConsumers { get; } = [];

    private HashSet<NotificationGroup> PoppedGroups { get; } = [];

    private HashSet<NotificationGroup> EnqueuedGroups { get; } = [];

    private List<NotificationPlayingTicket> PlayingTickets { get; } = [];

    private bool _isStopping = false;

    private bool CanDispatchRequests => !_isStopping && Settings.IsNotificationEnabled;

    public NotificationRequest? CurrentRequest { get; set; }

    
    public NotificationRequest GetRequest()
    {
        lock (_syncLock)
        {
            var group = RequestQueue.Peek();
            CurrentRequest = group.Head;
            return CurrentRequest;
        }
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
        if (NotificationProviders.Any(x => x.ProviderGuid == provider.ProviderGuid))
        {
            Logger.LogWarning("提醒提供方 {} 已被注册", provider.ProviderGuid);
            return;
        }
        
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
        IsNotificationsPlaying = PlayingTickets.Count > 0;
    }

    private void FinishNotificationPlaying(NotificationRequest request)
    {
        Logger.LogTrace("提醒 #{} 已播放完成", request.GetHashCode());
        lock (_syncLock)
        {
            UpdateNotificationPlayingState();
        }
    }

    public void TransitionRequestState(NotificationRequest request, NotificationState newState)
    {
        lock (_syncLock)
        {
            request.State = newState;
            switch (newState)
            {
                case NotificationState.Completed:
                case NotificationState.Cancelled:
                    EnqueuedGroups.Remove(request.Group);
                    break;
                case NotificationState.Queued:
                    PoppedGroups.Remove(request.Group);
                    break;
            }
            UpdateNotificationPlayingState();
        }
    }
    
    private void SetupNotificationRequest(NotificationRequest request, Guid providerGuid, Guid channelGuid)
    {
        if (request.NotificationSetupCompleted)
        {
            return;
        }
        request.NotificationSourceGuid = providerGuid;
        request.NotificationSource = (from i in NotificationProviders where i.ProviderGuid == providerGuid select i)
            .FirstOrDefault();
        request.ProviderSettings = request.NotificationSource?.ProviderSettings ?? request.ProviderSettings;
        if (request.InitialQueueIndex == -1)
        {
            request.InitialQueueIndex = Interlocked.Increment(ref _queueIndex);
        }
        
        if (channelGuid != Guid.Empty && request.ChannelId == Guid.Empty)
        {
            request.ChannelId = channelGuid;
        }

        channelGuid = request.ChannelId;

        var channel =
            request.NotificationSource?.NotificationChannels.FirstOrDefault(x => x.ProviderGuid == channelGuid);
        request.ChannelSettings = channel?.ProviderSettings;
        TransitionRequestState(request, NotificationState.Queued);
        request.NotificationSetupCompleted = true;
    }

    public void ShowNotification(NotificationRequest request, Guid providerGuid, Guid channelGuid, bool pushNotifications, bool isPlayed)
    {
        if (!Settings.IsNotificationEnabled)
        {
            request.CompletedTokenSource.Cancel();
            return;
        }
        if (!isPlayed)
        {
            SetupNotificationRequest(request, providerGuid, channelGuid);
            request.CompletedToken.Register(() => FinishNotificationPlaying(request));
        }
        var group = new NotificationGroup(request);
        request.Group = group;
        group.RegisterGroupCancellationPropagation();
        if (pushNotifications && PushNotificationGroups([group]))
        {
            UpdateNotificationPlayingState();
            return;
        }
        QueueNotificationGroup(group);
        PopGroupsToConsumers();
        UpdateNotificationPlayingState();
    }

    private void QueueNotificationGroup(NotificationGroup group)
    {
        lock (_syncLock)
        {
            if (!EnqueuedGroups.Add(group))
            {
                return;
            }
            RequestQueue.Enqueue(group, GetNotificationPriority(group.Head, false));
        }
    }

    private NotificationPriority GetNotificationPriority(NotificationRequest request, bool isPlayed)
    {
        return new NotificationPriority(
            Settings.NotificationProvidersPriority.IndexOf(request.NotificationSourceGuid.ToString()),
            request.InitialQueueIndex,
            request.IsPriorityOverride,
            isPlayed);
    }

    public async Task ShowNotificationAsync(NotificationRequest request, Guid providerGuid, Guid channelGuid)
    {
        var tcs = new TaskCompletionSource();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ShowNotification(request, providerGuid, channelGuid, true, false);
            request.CompletedToken.Register(() => tcs.TrySetResult());
        });
        await tcs.Task;
    }

    public void ShowChainedNotifications(NotificationRequest[] requests, Guid providerGuid, Guid channelGuid)
    {
        if (requests.Length <= 0)
        {
            return;
        }
        if (!Settings.IsNotificationEnabled)
        {
            foreach (var request in requests)
            {
                request.CompletedTokenSource.Cancel();
            }
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
        var group = new NotificationGroup(requests.ToList(), rootCancellationTokenSource, rootCompletedTokenSource);
        group.RegisterGroupCancellationPropagation();
        NotificationRequest? prevRequest = null;
        var head = requests[0];
        foreach (var request in requests)
        {
            request.Group = group;
            request.RootCancellationTokenSource = rootCancellationTokenSource;
            request.RootCompletedTokenSource = rootCompletedTokenSource;
            request.ChainedHeadRequest = head;
            if (prevRequest != null)
            {
                prevRequest.ChainedNextRequest = request;
            }
            SetupNotificationRequest(request, providerGuid, channelGuid);
            prevRequest = request;
            request.CompletedToken.Register(() => FinishNotificationPlaying(request));
        }

        if (PushNotificationGroups([group]))
        {
            return;
        }
        QueueNotificationGroup(group);
        PopGroupsToConsumers();
        UpdateNotificationPlayingState();
    }

    public async Task ShowChainedNotificationsAsync(NotificationRequest[] requests, Guid providerGuid, Guid channelGuid)
    {
        if (requests.Length <= 0)
        {
            return;
        }

        var tcs = new TaskCompletionSource();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ShowChainedNotifications(requests, providerGuid, channelGuid);
            requests.Last().CompletedToken.Register(() => tcs.TrySetResult());
        });
        await tcs.Task;
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
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_syncLock)
        {
            _isStopping = true;
        }
        CancelAllNotifications();
        return Task.CompletedTask;
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
        List<NotificationGroup> groups;
        List<NotificationPlayingTicket> playingTickets;
        
        lock (_syncLock)
        {
            groups = [];
            while (RequestQueue.Count > 0)
            {
                groups.Add(RequestQueue.Dequeue());
            }
            playingTickets = [.. PlayingTickets];
            EnqueuedGroups.Clear();
            PoppedGroups.Clear();
            PlayingTickets.Clear();
            UpdateNotificationPlayingState();
        }
        IAppHost.GetService<INotificationWorkerService>().CancelAllAudio();
        foreach (var group in groups)
        {
            foreach (var request in group.Requests)
            {
                request.CompletedTokenSource.Cancel();
            }
        }
        foreach (var ticket in playingTickets)
        {
            ticket.Request.CancellationTokenSource.Cancel();
            ticket.Request.CompletedTokenSource.Cancel();
        }
    }

    /// <summary>
    /// 清空请求队列并清理相关状态集合
    /// </summary>
    internal void ClearRequestQueue()
    {
        lock (_syncLock)
        {
            while (RequestQueue.Count > 0)
            {
                RequestQueue.Dequeue();
            }
            EnqueuedGroups.Clear();
            PoppedGroups.Clear();
        }
    }

    private NotificationConsumerRegisterInfo? RouteRequests(NotificationGroup group)
    {
        var activeRequests = group.CollectActiveRequests();
        if (activeRequests.Count <= 0)
        {
            return null;
        }

        var targetLine = activeRequests[0].TargetLineNumber;
        return RegisteredConsumers
            .FirstOrDefault(x => x.Consumer.AcceptsNotificationRequests &&
                                      x.Consumer.QueuedNotificationCount <= 0 &&
                                      (targetLine == null || x.LineNumber == targetLine));
    }

    private bool PushNotificationGroups(IReadOnlyList<NotificationGroup> groups)
    {
        Logger.LogTrace("开始推送提醒组 ({})", groups.Count);
        if (groups.Count <= 0)
        {
            return false;
        }

        NotificationConsumerRegisterInfo? consumer;
        List<NotificationPlayingTicket> tickets;
        lock (_syncLock)
        {
            if (!CanDispatchRequests)
            {
                Logger.LogTrace("存在未完成移交的提醒，暂缓推送");
                return false;
            }

            consumer = RouteRequests(groups[0]);
            if (consumer == null)
            {
                Logger.LogTrace("找不到接受提醒的提醒消费者");
                return false;
            }

            tickets = groups[0].CollectActiveRequests().Select(CreateTicket).ToList();
            Logger.LogTrace("将推送的提醒消费者：{}(#{})", consumer.Consumer, consumer.Consumer.GetHashCode());
            UpdateNotificationPlayingState();
        }
        consumer.Consumer.ReceiveNotifications(tickets);
        return true;
    }

    public IList<NotificationPlayingTicket> PullNotificationRequests(INotificationConsumer consumer)
    {
        lock (_syncLock)
        {
            if (!CanDispatchRequests)
            {
                return [];
            }

            var consumerInfo = RegisteredConsumers.FirstOrDefault(x => x.Consumer == consumer);
            if (consumerInfo == null)
            {
                return [];
            }

            while (RequestQueue.Count > 0)
            {
                var currentGroup = RequestQueue.Peek();
                if (PoppedGroups.Contains(currentGroup))
                {
                    RequestQueue.Dequeue();
                    EnqueuedGroups.Remove(currentGroup);
                    PoppedGroups.Remove(currentGroup);
                    continue;
                }

                var activeRequests = currentGroup.CollectActiveRequests();
                if (activeRequests.Count == 0)
                {
                    RequestQueue.Dequeue();
                    EnqueuedGroups.Remove(currentGroup);
                    continue;
                }

                var targetLine = activeRequests[0].TargetLineNumber;
                if (targetLine != null && consumerInfo.LineNumber != targetLine)
                {
                    return [];
                }

                var tickets = activeRequests.Select(CreateTicket).ToList();
                RequestQueue.Dequeue();
                EnqueuedGroups.Remove(currentGroup);
                PoppedGroups.Add(currentGroup);

                UpdateNotificationPlayingState();
                return tickets;
            }

            return [];
        }
    }

    public void PopGroupsToConsumers()
    {
        List<(NotificationConsumerRegisterInfo consumer, List<NotificationPlayingTicket> tickets)> batches = [];
        
        lock (_syncLock)
        {
            if (!CanDispatchRequests)
            {
                return;
            }

            var processedGroups = new HashSet<NotificationGroup>();
            while (RequestQueue.Count > 0)
            {
                var currentGroup = RequestQueue.Peek();
                if (PoppedGroups.Contains(currentGroup))
                {
                    RequestQueue.Dequeue();
                    EnqueuedGroups.Remove(currentGroup);
                    PoppedGroups.Remove(currentGroup);
                    continue;
                }
                if (!processedGroups.Add(currentGroup))
                {
                    break;
                }

                var activeRequests = currentGroup.CollectActiveRequests();
                if (activeRequests.Count == 0)
                {
                    RequestQueue.Dequeue();
                    EnqueuedGroups.Remove(currentGroup);
                    continue;
                }

                var consumer = RouteRequests(currentGroup);
                if (consumer == null)
                    break;
                var tickets = activeRequests.Select(CreateTicket).ToList();
                RequestQueue.Dequeue();
                EnqueuedGroups.Remove(currentGroup);
                PoppedGroups.Add(currentGroup);
                UpdateNotificationPlayingState();
                batches.Add((consumer, tickets));
            }
        }
        
        foreach (var (consumer, tickets) in batches)
        {
            consumer.Consumer.ReceiveNotifications(tickets);
        }
    }

    public void RegisterNotificationConsumer(INotificationConsumer consumer, int priority, int? lineNumber = null)
    {
        lock (_syncLock)
        {
            var registerInfo = new NotificationConsumerRegisterInfo(consumer, priority, lineNumber);

            for (var i = 0; i < RegisteredConsumers.Count; i++)
            {
                if (RegisteredConsumers[i].Priority <= registerInfo.Priority)
                    continue;
                RegisteredConsumers.Insert(i, registerInfo);
                return;
            }

            RegisteredConsumers.Add(registerInfo);
        }
        if (CanDispatchRequests)
        {
            PopGroupsToConsumers();
        }
    }

    public void UnregisterNotificationConsumer(INotificationConsumer consumer)
    {
        lock (_syncLock)
        {
            var registerInfo = RegisteredConsumers.FirstOrDefault(x => x.Consumer == consumer);
            if (registerInfo == null)
            {
                return;
            }

            RegisteredConsumers.Remove(registerInfo);
        }
        IAppHost.GetService<INotificationPlaybackService>().RemoveConsumer(consumer);
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

    private NotificationPlayingTicket CreateTicket(NotificationRequest request)
    {
        var ticket = NotificationWorkerService.CreateTicket(request);
        lock (_syncLock)
        {
            PlayingTickets.Add(ticket);
        }

        ticket.CancellationToken.Register(() =>
        {
            lock (_syncLock)
            {
                PlayingTickets.Remove(ticket);
                UpdateNotificationPlayingState();
            }

            _ = HandleTicketCancellationAsync(request, ticket);
        });
        request.CompletedToken.Register(() =>
        {
            lock (_syncLock)
            {
                PlayingTickets.Remove(ticket);
                UpdateNotificationPlayingState();
            }
        });
        return ticket;
    }

    private async Task HandleTicketCancellationAsync(NotificationRequest request, NotificationPlayingTicket ticket)
    {
        try
        {
            Logger.LogTrace("票据 {} 已取消，{}", ticket.GetHashCode(), request);
            try
            {
                await ticket.CancellationCompletedCompletionSource.Task;
            }
            catch (Exception ex)
            {
                Logger.LogTrace(ex, "等待取消完成时发生异常");
            }

            // 因为时态问题打的补丁.
            if (request.State == NotificationState.Playing)
            {
                var stateChangedSource = new TaskCompletionSource();
                PropertyChangedEventHandler handler = (_, args) =>
                {
                    if (args.PropertyName == nameof(NotificationRequest.State))
                        stateChangedSource.TrySetResult();
                };
                try
                {
                    request.PropertyChanged += handler;
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    using var _ = timeoutCts.Token.Register(() => stateChangedSource.TrySetResult());
                    await stateChangedSource.Task;
                }
                finally
                {
                    request.PropertyChanged -= handler;
                }
                Logger.LogTrace("票据State变更为 {}", request.State);
            }

            if (request.State != NotificationState.Interrupted)
            {
                return;
            }
            var group = request.Group;
            if (group == null)
            {
                Logger.LogWarning("提醒请求 {} 没有关联的组，无法重新入队", request);
                return;
            }
            if (request.State == NotificationState.Interrupted)
            {
                foreach (var r in group.Requests)
                {
                    if (r != request)
                    {
                        try { r.CancellationTokenSource.Cancel(); } catch (ObjectDisposedException) { }
                    }
                }
            }

            var activeRequests = group.CollectActiveRequests();
            if (activeRequests.Count > 0)
            {
                if (request.State == NotificationState.Interrupted)
                {
                    foreach (var r in activeRequests)
                    {
                        r.ResetCancellationTokensForTransfer();
                    }
                }
                foreach (var r in activeRequests)
                {
                    TransitionRequestState(r, NotificationState.Queued);
                }
                Logger.LogTrace("重新加入提醒队列 (组, {} 个活跃请求), {}", activeRequests.Count, request);
                QueueNotificationGroup(group);
                PopGroupsToConsumers();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理票据取消时发生异常");
        }
    }
    
    #region PropertyChanged
    
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

    #endregion
}
