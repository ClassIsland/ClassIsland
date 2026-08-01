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
using ClassIsland.Core.Extensions.UI;
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
public class NotificationHostService(SettingsService settingsService, ILogger<NotificationHostService> logger, INotificationWorkerService notificationWorkerService, IExactTimeService exactTimeService, INotificationBus notificationBus)
    : IHostedService, INotifyPropertyChanged, INotificationHostService
{
    private SettingsService SettingsService { get; } = settingsService;
    private ILogger<NotificationHostService> Logger { get; } = logger;
    private INotificationWorkerService NotificationWorkerService { get; } = notificationWorkerService;
    private IExactTimeService ExactTimeService { get; } = exactTimeService;
    private INotificationBus Bus { get; } = notificationBus;
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

    
    public NotificationRequest? GetRequest()
    {
        lock (_syncLock)
        {
            if (!RequestQueue.TryPeek(out var group, out _))
            {
                CurrentRequest = null;
                return null;
            }
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
            var oldState = request.Lifecycle.State;
            Logger.LogTrace("请求state变化: {RequestHash}, {OldState} -> {NewState}, Group={GroupId}",
                request.GetHashCode(), oldState, newState, request.Group?.GetHashCode().ToString() ?? "null");
            request.Lifecycle.State = newState;
            switch (newState)
            {
                case NotificationState.Completed:
                case NotificationState.Cancelled:
                    EnqueuedGroups.Remove(request.Group);
                    PoppedGroups.Remove(request.Group);
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
        Logger.LogTrace("显示提醒: {RequestHash}, Provider={ProviderGuid}, Channel={ChannelGuid}, Push={Push}, IsPlayed={IsPlayed}",
            request.GetHashCode(), providerGuid, channelGuid, pushNotifications, isPlayed);
        if (!Settings.IsNotificationEnabled)
        {
            request.Lifecycle.MarkCompleted();
            return;
        }
        if (!isPlayed)
        {
            SetupNotificationRequest(request, providerGuid, channelGuid);
            request.Lifecycle.CompletedToken.Register(() => FinishNotificationPlaying(request));
        }
        var group = new NotificationGroup(request);
        request.Group = group;
        group.RegisterGroupCancellationPropagation();
        if (pushNotifications)
        {
            if (PushNotificationGroups([group]))
            {
                UpdateNotificationPlayingState();
                return;
            }
        }
        QueueNotificationGroup(group);
        PopGroupsToConsumers();
        UpdateNotificationPlayingState();
    }

    private void QueueNotificationGroup(NotificationGroup group, bool isPlayed = false)
    {
        lock (_syncLock)
        {
            if (!EnqueuedGroups.Add(group))
            {
                return;
            }
            // 设置入队时间和有效分配时间
            group.EnqueuedAt = ExactTimeService.GetCurrentLocalDateTime();
            group.ValidUntil = group.Head.ValidityDuration.HasValue
                ? group.EnqueuedAt + group.Head.ValidityDuration.Value
                : null;
            var priority = GetNotificationPriority(group.Head, isPlayed);
            RequestQueue.Enqueue(group, priority);
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
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        request.Lifecycle.CompletedToken.Register(() => tcs.TrySetResult());
        await Dispatcher.UIThread.InvokeIfNeededAsync(() =>
        {
            ShowNotification(request, providerGuid, channelGuid, true, false);
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
                request.Lifecycle.MarkCompleted();
            }
            return;
        }

        var rootCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource([.. requests.Select(x => x.Lifecycle.CancellationTokenSource.Token)]);
        var rootCompletedTokenSource = CancellationTokenSource.CreateLinkedTokenSource([.. requests.Select(x => x.Lifecycle.CompletedTokenSource.Token)]);
        rootCancellationTokenSource.Token.Register(() =>
        {
            foreach (var request in requests.Where(x => !x.Lifecycle.CancellationToken.IsCancellationRequested))
            {
                request.Lifecycle.Cancel();
            }
        });
        rootCompletedTokenSource.Token.Register(() =>
        {
            try { rootCancellationTokenSource.Dispose(); } catch (ObjectDisposedException) { }
            try { rootCompletedTokenSource.Dispose(); } catch (ObjectDisposedException) { }
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
            request.Lifecycle.CompletedToken.Register(() => FinishNotificationPlaying(request));
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

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        requests.Last().Lifecycle.CompletedToken.Register(() => tcs.TrySetResult());
        await Dispatcher.UIThread.InvokeIfNeededAsync(() =>
        {
            ShowChainedNotifications(requests, providerGuid, channelGuid);
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
        Bus.StateChanged += OnBusStateChanged;
        Bus.ConsumerBecameIdle += OnBusConsumerBecameIdle;
        Bus.ConsumerRemoved += OnBusConsumerRemoved;
        return Task.CompletedTask;
    }

    private void OnBusStateChanged(NotificationRequest request, NotificationState from, NotificationState to)
    {
        TransitionRequestState(request, to);
    }

    private void OnBusConsumerBecameIdle(INotificationConsumer consumer)
    {
        PopGroupsToConsumers();
    }

    private void OnBusConsumerRemoved(INotificationConsumer consumer)
    {
        lock (_syncLock)
        {
            var registerInfo = RegisteredConsumers.FirstOrDefault(x => x.Consumer == consumer);
            if (registerInfo != null)
            {
                RegisteredConsumers.Remove(registerInfo);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_syncLock)
        {
            _isStopping = true;
        }
        Bus.StateChanged -= OnBusStateChanged;
        Bus.ConsumerBecameIdle -= OnBusConsumerBecameIdle;
        Bus.ConsumerRemoved -= OnBusConsumerRemoved;
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
        Logger.LogDebug("获取提醒提供方设置：{}", id);
        if (!Settings.NotificationProvidersSettings.TryGetValue(id.ToString(), out var o))
        {
            Logger.LogWarning("提醒提供方设置不存在：{}", id);
            return Activator.CreateInstance<T>();
        }
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
        NotificationWorkerService.CancelAllAudio();
        foreach (var group in groups)
        {
            foreach (var request in group.Requests)
            {
                request.Lifecycle.MarkCompleted();
            }
        }
        foreach (var ticket in playingTickets)
        {
            ticket.Request.Lifecycle.Cancel();
            ticket.Request.Lifecycle.MarkCompleted();
        }
    }

    /// <summary>
    /// 清空请求队列并清理相关状态集合
    /// </summary>
    internal void ClearRequestQueue()
    {
        List<NotificationGroup> dequeuedGroups;
        lock (_syncLock)
        {
            dequeuedGroups = [];
            while (RequestQueue.Count > 0)
            {
                dequeuedGroups.Add(RequestQueue.Dequeue());
            }
            EnqueuedGroups.Clear();
            PoppedGroups.Clear();
        }
        foreach (var group in dequeuedGroups)
        {
            foreach (var request in group.Requests)
            {
                try { request.Lifecycle.MarkCompleted(); } catch (ObjectDisposedException) { }
            }
        }
    }

    private NotificationConsumerRegisterInfo? RouteRequests(NotificationGroup group, HashSet<INotificationConsumer>? busyConsumers = null)
    {
        var activeRequests = group.CollectActiveRequests();
        if (activeRequests.Count <= 0)
        {
            return null;
        }

        var targetLine = activeRequests[0].TargetLineNumber;
        var result = RegisteredConsumers
            .FirstOrDefault(x => x.Consumer.AcceptsNotificationRequests &&
                                      x.Consumer.QueuedNotificationCount <= 0 &&
                                      (busyConsumers == null || !busyConsumers.Contains(x.Consumer)) &&
                                      (targetLine == null || x.LineNumber == targetLine));
        if (result != null)
        {
            Logger.LogTrace("通知组 {GroupId} 将路由到消费者 {ConsumerHash}",
                group.GetHashCode(), result.Consumer.GetHashCode());
        }
        return result;
    }

    private bool PushNotificationGroups(IReadOnlyList<NotificationGroup> groups)
    {
        if (groups.Count <= 0)
        {
            return false;
        }
        // Logger.LogTrace("开始推送 {GroupCount} 个提醒组", groups.Count);

        NotificationConsumerRegisterInfo? consumer;
        List<NotificationPlayingTicket> tickets;
        lock (_syncLock)
        {
            if (!CanDispatchRequests)
            {
                Logger.LogDebug("存在未完成移交的提醒, 无法分发");
                return false;
            }

            consumer = RouteRequests(groups[0]);
            if (consumer == null)
            {
                // Logger.LogDebug("提醒组 {GroupId} 无空闲消费者", groups[0].GetHashCode());
                return false;
            }

            tickets = groups[0].CollectActiveRequests().Select(CreateTicket).ToList();
            UpdateNotificationPlayingState();
        }
        consumer.Consumer.ReceiveNotifications(tickets);
        return true;
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
            var skippedGroups = new List<NotificationGroup>();
            var busyConsumers = new HashSet<INotificationConsumer>();
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
                if (currentGroup.ValidUntil.HasValue && ExactTimeService.GetCurrentLocalDateTime() > currentGroup.ValidUntil.Value)
                {
                    Logger.LogWarning("通知组 {GroupId} 已过期 (ValidUntil={ValidUntil}), 丢弃", currentGroup.GetHashCode(), currentGroup.ValidUntil.Value);
                    RequestQueue.Dequeue();
                    EnqueuedGroups.Remove(currentGroup);
                    foreach (var r in currentGroup.Requests)
                    {
                        try
                        {
                            r.Lifecycle.Cancel();
                            TransitionRequestState(r, NotificationState.Cancelled);
                        }
                        catch (ObjectDisposedException) { }
                    }
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

                var consumer = RouteRequests(currentGroup, busyConsumers);
                if (consumer == null)
                {
                    Logger.LogDebug("通知组 {GroupId} 无空闲消费者", currentGroup.GetHashCode());
                    skippedGroups.Add(RequestQueue.Dequeue());
                    continue;
                }

                busyConsumers.Add(consumer.Consumer);
                var tickets = activeRequests.Select(CreateTicket).ToList();
                RequestQueue.Dequeue();
                EnqueuedGroups.Remove(currentGroup);
                PoppedGroups.Add(currentGroup);
                UpdateNotificationPlayingState();
                batches.Add((consumer, tickets));
            }

            foreach (var g in skippedGroups)
            {
                RequestQueue.Enqueue(g, GetNotificationPriority(g.Head, true));
            }
        }

        foreach (var (consumer, tickets) in batches)
        {
            try
            {
                consumer.Consumer.ReceiveNotifications(tickets);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "向消费者 {ConsumerHash} 分发通知时发生异常", consumer.Consumer.GetHashCode());
                foreach (var ticket in tickets)
                {
                    try { ticket.Cancel(); } catch (ObjectDisposedException) { }
                }
            }
        }
    }

    public void RegisterNotificationConsumer(INotificationConsumer consumer, int priority, int? lineNumber = null)
    {
        lock (_syncLock)
        {
            if (RegisteredConsumers.Any(x => x.Consumer == consumer))
            {
                Logger.LogError("消费者 {ConsumerHash} 重复注册", consumer.GetHashCode());
                return;
            }

            var registerInfo = new NotificationConsumerRegisterInfo(consumer, priority, lineNumber);

            var inserted = false;
            for (var i = 0; i < RegisteredConsumers.Count; i++)
            {
                if (RegisteredConsumers[i].Priority <= registerInfo.Priority)
                    continue;
                RegisteredConsumers.Insert(i, registerInfo);
                inserted = true;
                break;
            }

            if (!inserted)
            {
                RegisteredConsumers.Add(registerInfo);
            }
        }
        if (CanDispatchRequests)
        {
            PopGroupsToConsumers();
        }
    }

    public void UnregisterNotificationConsumer(INotificationConsumer consumer)
    {
        Logger.LogDebug("消费者 {ConsumerHash} 已注销", consumer.GetHashCode());
        Bus.RaiseConsumerRemoved(consumer); // 发事件
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
        request.Lifecycle.CompletedToken.Register(() =>
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
            if (request.Lifecycle.State == NotificationState.Playing)
            {
                var stateChangedSource = new TaskCompletionSource();
                PropertyChangedEventHandler handler = (_, args) =>
                {
                    if (args.PropertyName == nameof(NotificationLifecycle.State))
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
                Logger.LogTrace("票据state变更为 {}", request.Lifecycle.State);
            }

            if (request.Lifecycle.State != NotificationState.Interrupted)
            {
                return;
            }
            var group = request.Group;
            if (group == null)
            {
                Logger.LogWarning("提醒请求 {} 没有关联的组，无法重新入队", request);
                return;
            }
            if (request.Lifecycle.State == NotificationState.Interrupted)
            {
                foreach (var r in group.Requests)
                {
                    if (r != request)
                    {
                        try { r.Lifecycle.Cancel(); } catch (ObjectDisposedException) { }
                    }
                }
            }

            var activeRequests = group.CollectActiveRequests();
            if (activeRequests.Count > 0)
            {
                if (request.Lifecycle.State == NotificationState.Interrupted)
                {
                    foreach (var r in activeRequests)
                    {
                        r.Lifecycle.ResetCancellationTokensForTransfer();
                    }
                }
                foreach (var r in activeRequests)
                {
                    TransitionRequestState(r, NotificationState.Queued);
                }
                group.ResetCancellationFlag();
                group.RegisterGroupCancellationPropagation();
                // Logger.LogTrace("重新加入提醒队列 (组, {} 个活跃请求), {}", activeRequests.Count, request);
                QueueNotificationGroup(group, true);
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
