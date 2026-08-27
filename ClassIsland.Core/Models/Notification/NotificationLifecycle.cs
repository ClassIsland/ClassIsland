using ClassIsland.Core.Enums.Notification;

namespace ClassIsland.Core.Models.Notification;

/// <summary>
/// 管理通知请求的状态机和生命周期 (取消,完成令牌,状态转换)
/// </summary>
public class NotificationLifecycle
{
    private readonly NotificationRequest _request;
    private readonly object _stateLock = new();
    private NotificationState _state = NotificationState.None;

    private static readonly HashSet<(NotificationState From, NotificationState To)> ValidStateTransitions =
    [
        (NotificationState.None, NotificationState.Queued),
        (NotificationState.Queued, NotificationState.Playing),
        (NotificationState.Queued, NotificationState.Cancelled),
        (NotificationState.Playing, NotificationState.Paused),
        (NotificationState.Playing, NotificationState.Completed),
        (NotificationState.Playing, NotificationState.Cancelled),
        (NotificationState.Playing, NotificationState.Interrupted),
        (NotificationState.Paused, NotificationState.Playing),
        (NotificationState.Paused, NotificationState.Cancelled),
        (NotificationState.Paused, NotificationState.Completed),
        (NotificationState.Paused, NotificationState.Interrupted),
        (NotificationState.None, NotificationState.Cancelled),
        (NotificationState.Interrupted, NotificationState.Queued), // 移交
        (NotificationState.Interrupted, NotificationState.Cancelled), // 中断后取消
    ];

    /// <summary>
    /// 初始化 <see cref="NotificationLifecycle"/> 实例
    /// </summary>
    /// <param name="request">关联的父请求</param>
    internal NotificationLifecycle(NotificationRequest request)
    {
        _request = request;
        CancellationTokenSource.Token.Register(() =>
        {
            Canceled?.Invoke(_request, EventArgs.Empty);
        });
        CompletedTokenSource.Token.Register(() =>
        {
            Completed?.Invoke(_request, EventArgs.Empty);
        });
    }

    /// <summary>
    /// 由 <see cref="NotificationRequest"/> 设置的回调
    /// 将属性变更转发到父请求
    /// </summary>
    internal Action<string?>? RaisePropertyChangedAction { get; set; }

    /// <summary>
    /// 通知被取消时触发
    /// </summary>
    public event EventHandler? Canceled;

    /// <summary>
    /// 通知显示完成时触发
    /// </summary>
    public event EventHandler? Completed;

    /// <summary>
    /// 通知播放状态
    /// </summary>
    public NotificationState State
    {
        get
        {
            lock (_stateLock)
            {
                return _state;
            }
        }
        internal set
        {
            lock (_stateLock)
            {
                if (_state == value) return;
                if (!IsValidStateTransition(_state, value))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[NotificationLifecycle] 非标准状态转换 {_state} -> {value}");
                    if (value != NotificationState.Cancelled)
                        return;
                }
                _state = value;
            }
            OnPropertyChanged();
        }
    }

    private static bool IsValidStateTransition(NotificationState from, NotificationState to)
    {
        return ValidStateTransitions.Contains((from, to));
    }

    /// <summary>
    /// 通知取消令牌源
    /// </summary>
    internal CancellationTokenSource CancellationTokenSource { get; private set; } = new();

    /// <summary>
    /// 通知完成令牌源
    /// </summary>
    internal CancellationTokenSource CompletedTokenSource { get; private set; } = new();

    /// <summary>
    /// 通知取消令牌
    /// </summary>
    public CancellationToken CancellationToken => CancellationTokenSource.Token;

    /// <summary>
    /// 通知完成令牌
    /// </summary>
    public CancellationToken CompletedToken => CompletedTokenSource.Token;

    private double _leftProgress = 1.0;

    /// <summary>
    /// 提醒播放剩余进度
    /// </summary>
    public double LeftProgress
    {
        get => _leftProgress;
        internal set
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                value = 0.0;
            if (value.Equals(_leftProgress)) return;
            _leftProgress = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 取消通知
    /// </summary>
    public void Cancel()
    {
        CancellationTokenSource.Cancel();
    }

    /// <summary>
    /// 标记通知已完成 (正常结束)
    /// </summary>
    public void MarkCompleted()
    {
        CompletedTokenSource.Cancel();
    }

    /// <summary>
    /// 标记通知已取消 (外部取消)
    /// 同 <see cref="Cancel"/>
    /// </summary>
    public void MarkCancelled()
    {
        CancellationTokenSource.Cancel();
    }

    /// <summary>
    /// 暂停通知
    /// </summary>
    public void Pause()
    {
        if (State == NotificationState.Playing)
        {
            State = NotificationState.Paused;
        }
    }

    /// <summary>
    /// 恢复通知
    /// </summary>
    public void Resume()
    {
        if (State == NotificationState.Paused)
        {
            State = NotificationState.Playing;
        }
    }

    /// <summary>
    /// 为移交重置取消令牌
    /// </summary>
    internal void ResetCancellationTokensForTransfer()
    {
        var oldCancelSource = CancellationTokenSource;
        var oldCompletedSource = CompletedTokenSource;
        CancellationTokenSource = new CancellationTokenSource();
        CancellationTokenSource.Token.Register(() =>
        {
            Canceled?.Invoke(_request, EventArgs.Empty);
        });
        CompletedTokenSource = new CancellationTokenSource();
        CompletedTokenSource.Token.Register(() =>
        {
            Completed?.Invoke(_request, EventArgs.Empty);
        });
        try { oldCancelSource.Dispose(); } catch (ObjectDisposedException) { }
        try { oldCompletedSource.Dispose(); } catch (ObjectDisposedException) { }
    }

    internal event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        RaisePropertyChangedAction?.Invoke(propertyName);
    }
}
