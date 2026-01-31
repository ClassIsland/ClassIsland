using ClassIsland.Shared.Abstraction.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Notification;

/// <summary>
/// 代表一个包含提醒播放状态的票据
/// </summary>
public class NotificationPlayingTicket : ObservableRecipient
{
    internal NotificationPlayingTicket()
    {
        
    }

    /// <summary>
    /// 控制当前提醒播放票据的 <see cref="CancellationTokenSource"/>
    /// </summary>
    public required CancellationTokenSource CancellationTokenSource { get; init; }

    /// <summary>
    /// 取消令牌
    /// </summary>
    public CancellationToken CancellationToken => CancellationTokenSource.Token;
        
    /// <summary>
    /// 遮罩播放任务
    /// </summary>
    public required Func<Task> ProcessMask { get; init; }
    
    /// <summary>
    /// 正文播放任务
    /// </summary>
    public required Func<Task> ProcessOverlay { get; init; }
    
    /// <summary>
    /// 关联的提醒请求
    /// </summary>
    public required NotificationRequest Request { get; init; }
    
    /// <summary>
    /// 该提醒请求实际要应用的提醒高级设置
    /// </summary>
    public required INotificationSettings Settings { get; init; }
    

    /// <summary>
    /// 取消本次播放任务。取消后，提醒将被退回至提醒主机，并移交给其它提醒消费者播放。
    /// </summary>
    public void Cancel()
    {
        CancellationTokenSource.Cancel();
    }
}