using System;
using System.Collections.Generic;
using System.Linq;
using ClassIsland.Core.Enums.Notification;

namespace ClassIsland.Core.Models.Notification;

/// <summary>
/// 将链式提醒请求作为单元管理。
/// </summary>
/// <remarks>
/// 初始化一个多请求（链式）组。
/// </remarks>
public class NotificationGroup(
    List<NotificationRequest> requests,
    CancellationTokenSource groupCts,
    CancellationTokenSource groupCompletedCts)
{
    /// <summary>
    /// 组内所有提醒请求。
    /// </summary>
    public List<NotificationRequest> Requests { get; } = requests;

    /// <summary>
    /// 组级别取消令牌源。
    /// </summary>
    public CancellationTokenSource GroupCancellationTokenSource { get; } = groupCts;

    /// <summary>
    /// 组级别完成令牌源。
    /// </summary>
    public CancellationTokenSource GroupCompletedTokenSource { get; } = groupCompletedCts;

    /// <summary>
    /// 初始化一个单请求组。
    /// </summary>
    public NotificationGroup(NotificationRequest request)
        : this([request],
            request.CancellationTokenSource,
            request.CompletedTokenSource)
    {
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
}
