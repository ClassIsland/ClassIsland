using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

/// <summary>
/// 应用挂起检测服务。
/// </summary>
/// <remarks>
/// 旧实现（上游）每 500 ms 用 <c>System.Timers.Timer</c> 投递一次
/// <see cref="Dispatcher.InvokeAsync(Action)"/> 探针，超时后既不取消已排队的探针，
/// 又用 <c>async void</c> 触发回调。界面一旦被拖住，探针会以 2 次/秒的速度在
/// 调度队列里堆积；界面恢复后要先排空这些积压探针，期间还会继续产生新探针，
/// 形成"越卡越久、恢复了也像没恢复"的现象；<c>async void</c> 在退出阶段抛出的
/// 异常还会直接终止进程。
///
/// 新实现做了四件事：
/// 1. 探针使用 <see cref="DispatcherPriority.Send"/>，优先于普通输入/布局任务执行；
/// 2. 超时后主动 <c>Abort()</c> 掉尚未执行的探针，队列不再堆积；
/// 3. 连续 <see cref="FailureThreshold"/> 次超时才判定挂起，避免 GC/大布局造成的误报；
/// 4. 全程捕获异常，退出阶段不再有未观察异常。
/// </remarks>
public class HangService : ObservableRecipient, IHangService
{
    /// <summary>探针间隔。</summary>
    private static readonly TimeSpan ProbeInterval = TimeSpan.FromMilliseconds(1000);

    /// <summary>单次探针等待上限。</summary>
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromMilliseconds(1500);

    /// <summary>连续多少次探针超时才判定为挂起。</summary>
    private const int FailureThreshold = 2;

    private readonly ILogger<HangService>? _logger;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;

    private bool _isHang;
    private bool _isChecking;
    private int _consecutiveFailures;
    private long _lastUiResponseTimestamp = Stopwatch.GetTimestamp();

    public HangService(ILogger<HangService>? logger = null)
    {
        _logger = logger;
        _loop = Task.Run(ProbeLoopAsync);
    }

    public bool IsHang
    {
        get => _isHang;
        set
        {
            if (value == _isHang) return;
            _isHang = value;
            OnPropertyChanged();
        }
    }

    public bool IsChecking
    {
        get => _isChecking;
        set
        {
            if (value == _isChecking) return;
            _isChecking = value;
            OnPropertyChanged();
        }
    }

    /// <summary>最近一次界面响应探针的耗时，用于诊断。</summary>
    public TimeSpan LastProbeLatency { get; private set; } = TimeSpan.Zero;

    /// <summary>
    /// 界面恢复响应后，从最后一次成功响应到现在经过的时间。
    /// </summary>
    public TimeSpan TimeSinceLastUiResponse =>
        Stopwatch.GetElapsedTime(_lastUiResponseTimestamp);

    public void AssumeHang()
    {
        // 仅作为"即将进入耗时操作"的提示，真正的挂起判定仍然由探针给出，
        // 避免构造函数里一次 AssumeHang 就把状态永久钉死在"挂起"。
        IsHang = true;
        _lastUiResponseTimestamp = Stopwatch.GetTimestamp();
    }

    private async Task ProbeLoopAsync()
    {
        var token = _cts.Token;
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(ProbeInterval, token).ConfigureAwait(false);
                await ProbeOnceAsync(token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常退出
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "界面响应探针循环异常退出。");
        }
    }

    private async Task ProbeOnceAsync(CancellationToken token)
    {
        var dispatcher = Dispatcher.UIThread;
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var startedAt = Stopwatch.GetTimestamp();
        DispatcherOperation? operation = null;

        try
        {
            IsChecking = true;

            // Send 优先级：排在输入与布局之前，不会被普通任务饿死。
            operation = dispatcher.InvokeAsync(() => tcs.TrySetResult(true), DispatcherPriority.Send);

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(ProbeTimeout, token))
                .ConfigureAwait(false) == tcs.Task;
            LastProbeLatency = Stopwatch.GetElapsedTime(startedAt);

            if (!completed)
            {
                // 关键：把还没执行的探针从调度队列里撤掉，避免卡死期间无限堆积。
                TryAbort(operation);
                _consecutiveFailures++;

                if (_consecutiveFailures >= FailureThreshold && !IsHang)
                {
                    IsHang = true;
                    _logger?.LogWarning(
                        "界面连续 {Count} 次未在 {Timeout} ms 内响应，标记为挂起（累计 {Elapsed} ms）。",
                        _consecutiveFailures, ProbeTimeout.TotalMilliseconds, TimeSinceLastUiResponse.TotalMilliseconds);
                }

                return;
            }

            _lastUiResponseTimestamp = Stopwatch.GetTimestamp();
            _consecutiveFailures = 0;

            if (IsHang)
            {
                IsHang = false;
                _logger?.LogInformation(
                    "界面已恢复响应，恢复判定耗时 {Elapsed} ms，最近一次探针延迟 {Latency} ms。",
                    TimeSinceLastUiResponse.TotalMilliseconds, LastProbeLatency.TotalMilliseconds);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // 退出阶段 dispatcher 已关闭时会抛 ObjectDisposedException 等异常，
            // 这里必须吞掉：旧实现的 async void 会把这类异常变成进程级崩溃。
            _logger?.LogDebug(ex, "界面响应探针执行失败（通常是应用正在退出）。");
        }
        finally
        {
            IsChecking = false;
        }
    }

    private static void TryAbort(DispatcherOperation operation)
    {
        try
        {
            if (operation.Status is DispatcherOperationStatus.Pending)
            {
                operation.Abort();
            }
        }
        catch
        {
            // Abort 与执行存在竞争，失败可以忽略：探针本身是幂等的。
        }
    }
}
