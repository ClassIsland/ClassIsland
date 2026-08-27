using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Core.Extensions.UI;
using ClassIsland.Shared;
using ClassIsland.Shared.Abstraction.Models;

namespace ClassIsland.Controls;

/// <summary>
/// 连接 <see cref="INotificationConsumer"/> <see cref="INotificationPlaybackHandler"/> 与 <see cref="MainWindowLine"/> UI
/// </summary>
public class MainWindowLineNotificationAdapter : INotificationConsumer, INotificationPlaybackHandler, IDisposable
{
    private readonly MainWindowLine _line;
    private readonly INotificationHostService _hostService;
    private readonly INotificationPlaybackService _playbackService;

    public MainWindowLineNotificationAdapter(
        MainWindowLine line,
        INotificationHostService hostService,
        INotificationPlaybackService playbackService)
    {
        _line = line ?? throw new ArgumentNullException(nameof(line));
        _hostService = hostService ?? throw new ArgumentNullException(nameof(hostService));
        _playbackService = playbackService ?? throw new ArgumentNullException(nameof(playbackService));
    }

    /// <inheritdoc />
    public void ReceiveNotifications(IReadOnlyList<NotificationPlayingTicket> notificationRequests)
    {
        if (_line.IsUnloading)
        {
            foreach (var ticket in notificationRequests)
            {
                ticket.Cancel();
            }
            return;
        }
        _playbackService.EnqueueAndPlay(this, this, notificationRequests);
    }

    /// <inheritdoc />
    public int QueuedNotificationCount => _playbackService.GetQueuedCount(this);

    /// <inheritdoc />
    public bool AcceptsNotificationRequests =>
        _line.IsNotificationEnabled && !_line.IsAllComponentsHid && !_line.IsUnloading;

    /// <inheritdoc />
    public async Task OnPlayMaskAsync(NotificationRequest request, INotificationSettings settings)
    {
        await Dispatcher.UIThread.InvokeIfNeededAsync(() => _line.ShowMask(request, settings));
    }

    /// <inheritdoc />
    public async Task OnPlayOverlayAsync(NotificationRequest request, INotificationSettings settings)
    {
        await Dispatcher.UIThread.InvokeIfNeededAsync(() => _line.ShowOverlay(request, settings));
    }

    /// <inheritdoc />
    public void OnPlaybackCompleted()
    {
        Dispatcher.UIThread.PostIfNeeded(() => _line.HideNotification());
    }

    /// <summary>
    /// 向通知主机注册消费者
    /// </summary>
    /// <param name="lineNumber">行号</param>
    /// <param name="isMainLine">是否为主行</param>
    public void Register(int lineNumber, bool isMainLine)
    {
        _hostService.RegisterNotificationConsumer(this, isMainLine ? -1 : 0, lineNumber);
    }

    /// <summary>
    /// 从通知主机注销消费者
    /// 将同步取消所有相关播放中的通知
    /// </summary>
    public void Unregister()
    {
        _hostService.UnregisterNotificationConsumer(this);
        _playbackService.CancelAll(this);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Unregister();
    }
}
