using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Notification;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

/// <summary>
/// 提醒播放服务
/// 协调提醒播放流程
/// </summary>
public class NotificationPlaybackService : INotificationPlaybackService
{
    private ILogger<NotificationPlaybackService> Logger { get; }
    private INotificationBus Bus { get; }

    private readonly Dictionary<INotificationConsumer, PlaybackSession> _sessions = new();
    private readonly object _syncLock = new();

    public NotificationPlaybackService(ILogger<NotificationPlaybackService> logger, INotificationBus bus)
    {
        Logger = logger;
        Bus = bus;
        Bus.ConsumerRemoved += OnConsumerRemoved;
    }

    private void OnConsumerRemoved(INotificationConsumer consumer)
    {
        RemoveConsumer(consumer);
    }

    private class PlaybackSession
    {
        public Queue<NotificationPlayingTicket> Queue { get; } = new();
        public List<NotificationPlayingTicket> PlayingTickets { get; } = new();
        public bool IsPlaying { get; set; }
        public INotificationPlaybackHandler? Handler { get; set; }
    }

    public void EnqueueAndPlay(INotificationConsumer consumer, INotificationPlaybackHandler handler, IEnumerable<NotificationPlayingTicket> tickets)
    {
        PlaybackSession session;
        lock (_syncLock)
        {
            if (!_sessions.TryGetValue(consumer, out session!))
            {
                session = new PlaybackSession();
                _sessions[consumer] = session;
            }
            session.Handler = handler;
            foreach (var ticket in tickets)
            {
                session.Queue.Enqueue(ticket);
            }

            if (session.IsPlaying)
            {
                return;
            }
            session.IsPlaying = true;
        }

        _ = StartPlaybackAsync(consumer, session);
    }

    public int GetQueuedCount(INotificationConsumer consumer)
    {
        lock (_syncLock)
        {
            // 路由层用这个值判断消费者是否空闲。
            var hasSession = _sessions.TryGetValue(consumer, out var session);
            var queueCount = hasSession ? session!.Queue.Count : 0;
            var playingCount = hasSession ? session!.PlayingTickets.Count : 0;
            var total = queueCount + playingCount;
            return total;
        }
    }

    public void CancelAll(INotificationConsumer consumer)
    {
        List<NotificationPlayingTicket> ticketsToCancel;
        lock (_syncLock)
        {
            if (!_sessions.TryGetValue(consumer, out var session))
            {
                return;
            }

            ticketsToCancel = [.. session.Queue, .. session.PlayingTickets];
            session.Queue.Clear();
            session.PlayingTickets.Clear();
        }

        foreach (var ticket in ticketsToCancel)
        {
            ticket.Cancel();
        }
    }

    public void RemoveConsumer(INotificationConsumer consumer)
    {
        CancelAll(consumer);
        lock (_syncLock)
        {
            _sessions.Remove(consumer);
        }
    }

    private async Task StartPlaybackAsync(INotificationConsumer consumer, PlaybackSession session)
    {
        try
        {
            while (true)
            {
                NotificationPlayingTicket ticket;
                INotificationPlaybackHandler handler;
                lock (_syncLock)
                {
                    if (session.Queue.Count == 0)
                    {
                        session.IsPlaying = false;
                        var h = session.Handler;
                        _sessions.Remove(consumer);
                        h?.OnPlaybackCompleted();
                        Bus.RaiseConsumerBecameIdle(consumer);
                        return;
                    }
                    ticket = session.Queue.Dequeue();
                    session.PlayingTickets.Add(ticket);
                    handler = session.Handler!;
                }
                try
                {
                    await PlayTicketAsync(ticket, handler);
                }
                catch (OperationCanceledException)
                {
                    Logger.LogInformation("提醒票据已取消 tid={ticketId}, request={requestId}",
                        ticket.GetHashCode(), ticket.Request.GetHashCode());
                }
                finally
                {
                    lock (_syncLock)
                    {
                        session.PlayingTickets.Remove(ticket);
                    }
                    Bus.RaiseConsumerBecameIdle(consumer);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "提醒播放会话出现异常 (消费者 {ConsumerHash})", consumer.GetHashCode());
            lock (_syncLock)
            {
                session.IsPlaying = false;
                _sessions.Remove(consumer);
            }
            try { session.Handler?.OnPlaybackCompleted(); } catch { }
            Bus.RaiseConsumerBecameIdle(consumer);
        }
    }

    private async Task PlayTicketAsync(NotificationPlayingTicket ticket, INotificationPlaybackHandler handler)
    {
        var request = ticket.Request;
        var settings = ticket.Settings;
        var cancellationToken = ticket.CancellationTokenSource.Token;

        try
        {
            var hasMask = request.MaskContent.Duration > TimeSpan.Zero;
            var hasOverlay = request.OverlayContent != null && request.OverlayContent.Duration > TimeSpan.Zero;

            if (hasMask && !cancellationToken.IsCancellationRequested)
            {
                await handler.OnPlayMaskAsync(request, settings);
                await ticket.ProcessMask();

                if (hasOverlay && !cancellationToken.IsCancellationRequested)
                {
                    await handler.OnPlayOverlayAsync(request, settings);
                    await ticket.ProcessOverlay();
                }
            }
            else if (hasOverlay && !cancellationToken.IsCancellationRequested)
            {
                await handler.OnPlayOverlayAsync(request, settings);
                await ticket.ProcessOverlay();
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogTrace("提醒票据被取消：tid={ticketId}, {}", ticket.GetHashCode(), request);
            throw;
        }
    }
}
