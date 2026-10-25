using System.Diagnostics;
using System.Threading.Channels;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls.NotificationTemplates;
using ClassIsland.Core.Enums.Notification;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Core.Models.Notification.Templates;
using ClassIsland.iOS.Services.Platform;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Services;
using UserNotifications;

namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// 完成已由课程排程接管的票据，并将其余提醒链折叠为即时 iOS 通知。
/// </summary>
internal sealed class IosNotificationQueueConsumer : INotificationConsumer, IDisposable
{
    private const string ImmediateCategoryIdentifier = "classisland.notifications";
    private const int MaximumQueuedNotifications = 64;
    private static readonly TimeSpan ScheduleMatchTolerance = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan FallbackCapacityRetryInterval =
        TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan MaximumFallbackCapacityWait =
        TimeSpan.FromSeconds(5);
    private static readonly TimeSpan CapacityExhaustionProbeInterval =
        TimeSpan.FromSeconds(30);

    private readonly IosNotificationAuthorizationService _authorizationService;
    private readonly SettingsService _settingsService;
    private readonly INotificationHostService _notificationHostService;
    private readonly IExactTimeService _exactTimeService;
    private readonly IosNotificationMutationGate _notificationMutationGate;
    private readonly IosFallbackCapacityBacklogGate _capacityBacklogGate = new();
    private readonly Channel<QueuedNotification> _queue =
        Channel.CreateBounded<QueuedNotification>(new BoundedChannelOptions(
            MaximumQueuedNotifications)
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Task _worker;
    private IosLessonNotificationRequest[] _scheduledRequests = [];
    private int _queuedNotificationCount;
    private int _disposed;

    public IosNotificationQueueConsumer(
        IosNotificationAuthorizationService authorizationService,
        SettingsService settingsService,
        INotificationHostService notificationHostService,
        IExactTimeService exactTimeService,
        IosNotificationMutationGate notificationMutationGate)
    {
        _authorizationService = authorizationService;
        _settingsService = settingsService;
        _notificationHostService = notificationHostService;
        _exactTimeService = exactTimeService;
        _notificationMutationGate = notificationMutationGate;
        _worker = Task.Run(ProcessQueueAsync);
    }

    internal void SetScheduledRequests(
        IReadOnlyCollection<IosLessonNotificationRequest> requests,
        bool hasFallbackCapacity)
    {
        Volatile.Write(ref _scheduledRequests, requests.ToArray());
        if (hasFallbackCapacity)
        {
            _capacityBacklogGate.MarkCapacityAvailable();
        }
    }

    internal void ClearScheduledRequests() =>
        Volatile.Write(ref _scheduledRequests, []);

    public int QueuedNotificationCount =>
        Volatile.Read(ref _queuedNotificationCount);

    public bool AcceptsNotificationRequests => Volatile.Read(ref _disposed) == 0;

    public void ReceiveNotifications(
        IReadOnlyList<NotificationPlayingTicket> notificationRequests)
    {
        if (notificationRequests.Count == 0)
        {
            return;
        }

        if (!Dispatcher.UIThread.CheckAccess())
        {
            var requests = notificationRequests.ToArray();
            Dispatcher.UIThread.Post(() => ReceiveNotifications(requests));
            return;
        }

        if (Volatile.Read(ref _disposed) != 0)
        {
            foreach (var ticket in notificationRequests)
            {
                CompleteTicketCore(ticket);
            }
            return;
        }

        var ticketCount = notificationRequests.Count;
        _ = Interlocked.Add(
            ref _queuedNotificationCount,
            ticketCount);

        var chains = notificationRequests
            .GroupBy(
                x => x.Request.ChainedHeadRequest ?? x.Request,
                ReferenceEqualityComparer.Instance)
            .Select(x => x.ToArray())
            .ToArray();
        foreach (var tickets in chains)
        {
            QueuedNotification notification;
            try
            {
                notification = CreateQueuedNotification(tickets);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"准备 iOS 即时通知内容时发生异常：{exception}");
                CompleteReservedTicketsCore(tickets);
                continue;
            }

            if (Volatile.Read(ref _disposed) == 0 &&
                _queue.Writer.TryWrite(notification))
            {
                continue;
            }

            CompleteNotificationCore(notification);
        }
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            await foreach (var notification in _queue.Reader.ReadAllAsync(_cancellation.Token))
            {
                await ProcessNotificationAsync(notification, _cancellation.Token);
            }
        }
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
        {
            // Dispose 正在停止消费者。
        }
        finally
        {
            while (_queue.Reader.TryRead(out var notification))
            {
                await CompleteNotificationAsync(notification);
            }
        }
    }

    private async Task ProcessNotificationAsync(
        QueuedNotification notification,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!HasActiveTicket(notification))
            {
                return;
            }

            var authorized = await _authorizationService
                .RequestAuthorizationIfNeededAsync()
                .WaitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!HasActiveTicket(notification))
            {
                return;
            }

            if (!authorized)
            {
                Console.Error.WriteLine("iOS/iPadOS 通知权限未授予，无法显示即时提醒。");
                return;
            }

            await SubmitImmediateNotificationAsync(notification, cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested ||
                  !HasActiveTicket(notification))
        {
            // 消费者停止或提醒链已取消，不再投递过期通知。
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"提交 iOS 即时通知时发生异常：{exception}");
        }
        finally
        {
            await CompleteNotificationAsync(notification);
        }
    }

    private async Task SubmitImmediateNotificationAsync(
        QueuedNotification notification,
        CancellationToken cancellationToken)
    {
        if (!_capacityBacklogGate.CanWaitForCapacity(DateTimeOffset.UtcNow))
        {
            return;
        }

        var identifier =
            $"{IosNotificationCapacityPolicy.ImmediateFallbackIdentifierPrefix}{Guid.NewGuid():N}";
        var capacityWaitStarted = Stopwatch.GetTimestamp();
        while (true)
        {
            var completed = await _notificationMutationGate.ExecuteAsync(
                () => TrySubmitImmediateNotificationAsync(
                    identifier,
                    notification,
                    Stopwatch.GetElapsedTime(capacityWaitStarted),
                    cancellationToken),
                cancellationToken);
            if (completed)
            {
                return;
            }

            await Task.Delay(FallbackCapacityRetryInterval, cancellationToken);
        }
    }

    private async Task<bool> TrySubmitImmediateNotificationAsync(
        string identifier,
        QueuedNotification notification,
        TimeSpan capacityWaitElapsed,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!HasActiveTicket(notification) ||
            IsCoveredBySystemSchedule(notification))
        {
            return true;
        }

        var notificationCenter = UNUserNotificationCenter.Current;
        var pending = await notificationCenter.GetPendingNotificationRequestsAsync() ?? [];
        cancellationToken.ThrowIfCancellationRequested();
        var capacityDecision = IosNotificationCapacityPolicy
            .GetFallbackSubmissionDecision(
                identifier,
                pending.Select(x => x.Identifier),
                capacityWaitElapsed,
                MaximumFallbackCapacityWait);
        if (capacityDecision == IosFallbackNotificationCapacityDecision.Retry)
        {
            return false;
        }
        if (capacityDecision ==
            IosFallbackNotificationCapacityDecision.CapacityExhausted)
        {
            _capacityBacklogGate.MarkCapacityExhausted(
                DateTimeOffset.UtcNow,
                CapacityExhaustionProbeInterval);
            throw new TimeoutException(
                $"iOS 的 {IosNotificationCapacityPolicy.MaximumPendingNotificationCount} " +
                $"个待处理本地通知槽在 {MaximumFallbackCapacityWait.TotalSeconds:0} 秒内均未释放。" +
                "当前积压中的其它即时提醒将直接完成，避免逐条重复等待。");
        }

        _capacityBacklogGate.MarkCapacityAvailable();

        var activeTickets = GetActiveTickets(notification);
        if (activeTickets.Length == 0 ||
            IsCoveredBySystemSchedule(activeTickets))
        {
            return true;
        }

        var payload = IosFallbackNotificationPayloadPolicy.Create(
            notification.ProviderName,
            activeTickets.Select(x => x.Text));
        using var content = new UNMutableNotificationContent
        {
            Title = payload.Title,
            Body = payload.Body,
            CategoryIdentifier = ImmediateCategoryIdentifier,
            ThreadIdentifier = ImmediateCategoryIdentifier
        };
        if (IosFallbackNotificationPayloadPolicy.ShouldPlaySound(
                _settingsService.Settings.AllowNotificationSound,
                activeTickets.Select(x => x.NotificationSoundEnabled)))
        {
            content.Sound = UNNotificationSound.Default;
        }

        using var request = UNNotificationRequest.FromIdentifier(
            identifier,
            content,
            null);
        cancellationToken.ThrowIfCancellationRequested();
        if (!HasActiveTicket(notification) ||
            IsCoveredBySystemSchedule(notification))
        {
            return true;
        }

        await notificationCenter.AddNotificationRequestAsync(request);
        return true;
    }

    private QueuedNotification CreateQueuedNotification(
        IReadOnlyList<NotificationPlayingTicket> tickets)
    {
        var firstTicket = tickets[0];
        var logicalNow = _exactTimeService.GetCurrentLocalDateTime();
        var systemNow = DateTimeOffset.Now;
        var queuedTickets = tickets.Select(ticket =>
        {
            var chainedHead = ticket.Request.ChainedHeadRequest;
            return new QueuedTicket(
                ticket,
                new IosFallbackNotificationTextEntry(
                    GetText(ticket.Request.MaskContent),
                    GetText(ticket.Request.OverlayContent)),
                ticket.Settings.IsNotificationSoundEnabled,
                new ScheduledTicketMatch(
                    ticket.Request.NotificationSourceGuid,
                    ticket.Request.ChannelId,
                    IosNotificationSchedulingPolicy.GetExpectedQueueTicketLocalFireTime(
                        ticket.Request.ChannelId,
                        chainedHead != null &&
                        !ReferenceEquals(chainedHead, ticket.Request),
                        chainedHead?.OverlayContent?.EndTime,
                        logicalNow,
                        systemNow)));
        }).ToArray();

        return new QueuedNotification(
            firstTicket.Request.NotificationSource?.Name,
            queuedTickets);
    }

    private static string? GetText(NotificationContent? content)
    {
        if (content == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(content.SpeechContent))
        {
            return content.SpeechContent.Trim();
        }

        var text = content.Content switch
        {
            string value => value,
            SimpleTextTemplateData value => value.Text,
            TwoIconsMaskTemplateData value => value.Text,
            RollingTextTemplate
            {
                DataContext: RollingTextTemplateData value
            } => value.Text,
            _ => null
        };
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private bool IsCoveredBySystemSchedule(QueuedNotification notification) =>
        IsCoveredBySystemSchedule(GetActiveTickets(notification));

    private bool IsCoveredBySystemSchedule(
        IReadOnlyCollection<QueuedTicket> activeTickets)
    {
        if (activeTickets.Count == 0)
        {
            return true;
        }

        var scheduledRequests = Volatile.Read(ref _scheduledRequests);
        return activeTickets.All(ticket =>
            IosNotificationSchedulingPolicy.CanCompleteQueueTicket(
                ticket.ScheduleMatch.ProviderId,
                ticket.ScheduleMatch.ChannelId,
                ticket.ScheduleMatch.ExpectedLocalFireTime,
                scheduledRequests,
                ScheduleMatchTolerance));
    }

    private static bool HasActiveTicket(QueuedNotification notification) =>
        notification.Tickets.Any(x => IsTicketActive(x.Ticket));

    private static QueuedTicket[] GetActiveTickets(QueuedNotification notification) =>
        notification.Tickets
            .Where(x => IsTicketActive(x.Ticket))
            .ToArray();

    private static bool IsTicketActive(NotificationPlayingTicket ticket) =>
        !ticket.CancellationToken.IsCancellationRequested &&
        !ticket.Request.CancellationToken.IsCancellationRequested &&
        !ticket.Request.CompletedToken.IsCancellationRequested;

    private async Task CompleteNotificationAsync(QueuedNotification notification)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            CompleteNotificationCore(notification);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => CompleteNotificationCore(notification));
    }

    private void CompleteNotificationCore(QueuedNotification notification) =>
        CompleteReservedTicketsCore(notification.Tickets.Select(x => x.Ticket).ToArray());

    private void CompleteReservedTicketsCore(
        IReadOnlyCollection<NotificationPlayingTicket> tickets)
    {
        try
        {
            foreach (var ticket in tickets)
            {
                try
                {
                    CompleteTicketCore(ticket);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"完成 iOS 桌面提醒票据时发生异常：{exception}");
                }
            }
        }
        finally
        {
            ReleaseQueueSlotsAndPull(tickets.Count);
        }
    }

    private void ReleaseQueueSlotsAndPull(int releasedTicketCount)
    {
        if (Interlocked.Add(
                ref _queuedNotificationCount,
                -releasedTicketCount) != 0 ||
            Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        var nextRequests = _notificationHostService.PullNotificationRequests();
        if (nextRequests.Count > 0)
        {
            ReceiveNotifications(nextRequests.ToArray());
        }
    }

    private static void CompleteTicketCore(NotificationPlayingTicket ticket)
    {
        ticket.Request.State = NotificationState.Completed;
        try
        {
            ticket.Request.CompletedTokenSource.Cancel();
        }
        catch (AggregateException exception)
        {
            // CancellationToken 回调由提醒提供方拥有；单个回调异常不能阻止
            // 其余票据从 iOS 的桌面提醒队列中完成。
            Console.Error.WriteLine($"完成 iOS 桌面提醒票据时回调失败：{exception}");
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _queue.Writer.TryComplete();
        _cancellation.Cancel();
        _ = DisposeWorkerAsync();
    }

    private async Task DisposeWorkerAsync()
    {
        try
        {
            await _worker;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"停止 iOS 即时通知消费者时发生异常：{exception}");
        }
        finally
        {
            _cancellation.Dispose();
        }
    }

    private sealed record QueuedNotification(
        string? ProviderName,
        QueuedTicket[] Tickets);

    private sealed record QueuedTicket(
        NotificationPlayingTicket Ticket,
        IosFallbackNotificationTextEntry Text,
        bool NotificationSoundEnabled,
        ScheduledTicketMatch ScheduleMatch);

    private sealed record ScheduledTicketMatch(
        Guid ProviderId,
        Guid ChannelId,
        DateTime ExpectedLocalFireTime);
}
