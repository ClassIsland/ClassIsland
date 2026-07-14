using System.Threading.Channels;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Controls.NotificationTemplates;
using ClassIsland.Core.Enums.Notification;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Core.Models.Notification.Templates;
using ClassIsland.iOS.Services.Platform;
using ClassIsland.Services;
using ClassIsland.Services.NotificationProviders;
using UserNotifications;

namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// 课程提醒由长期本地计划负责；其它共享提醒转换为即时 iOS 通知。
/// </summary>
internal sealed class IosNotificationQueueConsumer : INotificationConsumer, IDisposable
{
    private const string ImmediateCategoryIdentifier = "classisland.notifications";
    private const int MaximumQueuedNotifications = 64;

    private static readonly HashSet<Guid> ScheduledLessonChannelIds =
    [
        Guid.Parse(ClassNotificationProvider.PrepareOnClassChannelId),
        Guid.Parse(ClassNotificationProvider.OnClassChannelId),
        Guid.Parse(ClassNotificationProvider.OnBreakingChannelId)
    ];

    private readonly IosNotificationAuthorizationService _authorizationService;
    private readonly SettingsService _settingsService;
    private readonly INotificationHostService _notificationHostService;
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
    private int _queuedNotificationCount;
    private int _disposed;

    public IosNotificationQueueConsumer(
        IosNotificationAuthorizationService authorizationService,
        SettingsService settingsService,
        INotificationHostService notificationHostService)
    {
        _authorizationService = authorizationService;
        _settingsService = settingsService;
        _notificationHostService = notificationHostService;
        _worker = Task.Run(ProcessQueueAsync);
    }

    public int QueuedNotificationCount =>
        Volatile.Read(ref _queuedNotificationCount);

    public bool AcceptsNotificationRequests => Volatile.Read(ref _disposed) == 0;

    public void ReceiveNotifications(
        IReadOnlyList<NotificationPlayingTicket> notificationRequests)
    {
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

        Interlocked.Add(ref _queuedNotificationCount, notificationRequests.Count);
        foreach (var ticket in notificationRequests)
        {
            QueuedNotification notification;
            try
            {
                notification = CreateQueuedNotification(ticket);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"准备 iOS 即时通知内容时发生异常：{exception}");
                CompleteReservedTicketCore(ticket);
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
                await ProcessTicketAsync(notification, _cancellation.Token);
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

    private async Task ProcessTicketAsync(
        QueuedNotification notification,
        CancellationToken cancellationToken)
    {
        var ticket = notification.Ticket;
        try
        {
            if (notification.IsScheduledLesson ||
                ticket.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            var authorized = await _authorizationService
                .RequestAuthorizationIfNeededAsync()
                .WaitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (ticket.CancellationToken.IsCancellationRequested)
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
                  ticket.CancellationToken.IsCancellationRequested)
        {
            // 消费者停止或票据已取消，不再投递过期通知。
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
        var ticket = notification.Ticket;
        cancellationToken.ThrowIfCancellationRequested();
        if (ticket.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        using var content = new UNMutableNotificationContent
        {
            Title = notification.Title,
            Body = notification.Body,
            CategoryIdentifier = ImmediateCategoryIdentifier,
            ThreadIdentifier = ImmediateCategoryIdentifier
        };
        if (notification.PlaySound)
        {
            content.Sound = UNNotificationSound.Default;
        }

        using var request = UNNotificationRequest.FromIdentifier(
            $"classisland.notification.{Guid.NewGuid():N}",
            content,
            null);
        cancellationToken.ThrowIfCancellationRequested();
        if (ticket.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        await UNUserNotificationCenter.Current.AddNotificationRequestAsync(request);
    }

    private QueuedNotification CreateQueuedNotification(
        NotificationPlayingTicket ticket)
    {
        if (ScheduledLessonChannelIds.Contains(ticket.Request.ChannelId))
        {
            return new QueuedNotification(ticket, true, string.Empty, string.Empty, false);
        }

        var title = GetText(ticket.Request.MaskContent, "ClassIsland 提醒");
        var body = GetText(
            ticket.Request.OverlayContent,
            ticket.Request.OverlayContent == null
                ? string.Empty
                : "请打开 ClassIsland 查看提醒详情。");
        if (string.Equals(title, body, StringComparison.Ordinal))
        {
            body = string.Empty;
        }

        return new QueuedNotification(
            ticket,
            false,
            title,
            body,
            _settingsService.Settings.AllowNotificationSound &&
            ticket.Settings.IsNotificationSoundEnabled);
    }

    private static string GetText(NotificationContent? content, string fallback)
    {
        if (content == null)
        {
            return fallback;
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
        return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
    }

    private async Task CompleteNotificationAsync(QueuedNotification notification)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            CompleteNotificationCore(notification);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => CompleteNotificationCore(notification));
    }

    private void CompleteNotificationCore(QueuedNotification notification)
    {
        CompleteReservedTicketCore(notification.Ticket);
    }

    private void CompleteReservedTicketCore(NotificationPlayingTicket ticket)
    {
        try
        {
            CompleteTicketCore(ticket);
        }
        finally
        {
            ReleaseQueueSlotAndPull();
        }
    }

    private void ReleaseQueueSlotAndPull()
    {
        if (Interlocked.Decrement(ref _queuedNotificationCount) != 0 ||
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
        NotificationPlayingTicket Ticket,
        bool IsScheduledLesson,
        string Title,
        string Body,
        bool PlaySound);
}
