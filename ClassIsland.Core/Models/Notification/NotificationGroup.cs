using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ClassIsland.Core.Enums.Notification;

namespace ClassIsland.Core.Models.Notification;

/// <summary>
/// 将链式提醒请求作为单元管理。
/// </summary>
public class NotificationGroup
{
    private int _groupCancelled;

    /// <summary>
    /// 组内所有提醒请求。
    /// </summary>
    public List<NotificationRequest> Requests { get; }

    /// <summary>
    /// 组级别取消令牌源。取消此令牌将触发组内所有请求取消。
    /// </summary>
    public CancellationTokenSource GroupCancellationTokenSource { get; }

    /// <summary>
    /// 组级别完成令牌源。
    /// </summary>
    public CancellationTokenSource GroupCompletedTokenSource { get; }

    /// <summary>
    /// 入队时间。由 QueueNotificationGroup 设置。
    /// </summary>
    public DateTime EnqueuedAt { get; internal set; } = DateTime.MinValue;

    /// <summary>
    /// 有效分配时间。超过此时间未被分配则丢弃。为 null 表示无时效限制。
    /// </summary>
    public DateTime? ValidUntil { get; internal set; }

    /// <summary>
    /// 初始化一个多请求（链式）组。
    /// </summary>
    public NotificationGroup(
        List<NotificationRequest> requests,
        CancellationTokenSource groupCts,
        CancellationTokenSource groupCompletedCts)
    {
        Requests = requests;
        GroupCancellationTokenSource = groupCts;
        GroupCompletedTokenSource = groupCompletedCts;
    }

    /// <summary>
    /// 初始化一个单请求组。
    /// </summary>
    public NotificationGroup(NotificationRequest request)
    {
        Requests = [request];
        GroupCancellationTokenSource = new CancellationTokenSource();
        GroupCompletedTokenSource = request.CompletedTokenSource;
    }

    /// <summary>
    /// 组内第一个请求。
    /// </summary>
    public NotificationRequest Head => Requests[0];

    /// <summary>
    /// 收集组内所有活跃（未完成、未取消）的请求。
    /// </summary>
    public List<NotificationRequest> CollectActiveRequests()
    {
        return Requests
            .Where(r => r.State != NotificationState.Completed &&
                        r.State != NotificationState.Cancelled)
            .ToList();
    }

    /// <summary>
    /// 取消组内所有请求。
    /// </summary>
    public void CancelAll()
    {
        if (Interlocked.CompareExchange(ref _groupCancelled, 1, 0) != 0)
            return;
        foreach (var r in Requests)
        {
            try { r.CancellationTokenSource.Cancel(); } catch (ObjectDisposedException) { }
        }
    }

    /// <summary>
    /// 取消传播。
    /// </summary>
    public void RegisterGroupCancellationPropagation()
    {
        foreach (var request in Requests)
        {
            request.CancellationTokenSource.Token.Register(() =>
            {
                CancelAll();
            });
        }
    }
}
