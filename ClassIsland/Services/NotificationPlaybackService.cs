using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Notification;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class NotificationPlaybackService(ILogger<NotificationPlaybackService> logger, INotificationHostService notificationHostService) : INotificationPlaybackService
{
    private ILogger<NotificationPlaybackService> Logger { get; } = logger;
    private INotificationHostService NotificationHostService { get; } = notificationHostService;

    private readonly Dictionary<INotificationConsumer, PlaybackSession> _sessions = new();
    private readonly object _syncLock = new();

    private class PlaybackSession
    {
        public Queue<NotificationPlayingTicket> Queue { get; } = new();
        public List<NotificationPlayingTicket> PlayingTickets { get; } = new();
        public bool IsPlaying { get; set; }
        public INotificationPlaybackHandler? Handler { get; set; }
    }

    private static bool ContainsRequest(PlaybackSession session, NotificationRequest request) =>
        session.PlayingTickets.Any(x => x.Request == request) ||
        session.Queue.Any(x => x.Request == request);

    private void EnqueueTicketsNoDuplicate(PlaybackSession session, IEnumerable<NotificationPlayingTicket> tickets)
    {
        foreach (var ticket in tickets)
        {
            if (ContainsRequest(session, ticket.Request))
                continue;

            session.Queue.Enqueue(ticket);
        }
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
            EnqueueTicketsNoDuplicate(session, tickets);

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
            return _sessions.TryGetValue(consumer, out var session) ? session.Queue.Count : 0;
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
                        session.Handler?.OnPlaybackCompleted();
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
                }

                var newTickets = NotificationHostService.PullNotificationRequests(consumer);
                if (newTickets.Any())
                {
                    lock (_syncLock)
                    {
                        EnqueueTicketsNoDuplicate(session, newTickets);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "提醒播放会话出现异常。");
            lock (_syncLock)
            {
                session.IsPlaying = false;
            }
        }
    }

    private async Task PlayTicketAsync(NotificationPlayingTicket ticket, INotificationPlaybackHandler handler)
    {
        var request = ticket.Request;
        var settings = ticket.Settings;
        var cancellationToken = ticket.CancellationTokenSource.Token;

        try
        {
            if (request.MaskContent.Duration > TimeSpan.Zero && !cancellationToken.IsCancellationRequested)
            {
                await handler.OnPlayMaskAsync(request, settings);
                await ticket.ProcessMask();

                if (request.OverlayContent != null && !cancellationToken.IsCancellationRequested && request.OverlayContent.Duration > TimeSpan.Zero)
                {
                    await handler.OnPlayOverlayAsync(request, settings);
                    await ticket.ProcessOverlay();
                }
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogTrace("提醒票据被取消：tid={ticketId}, {}", ticket.GetHashCode(), request);
            throw;
        }
    }
}
