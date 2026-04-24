using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

/// <summary>
/// 应用程序内存监视服务，用于在内存使用超出<see cref="MemoryLimitBytes"/>时重启应用程序，缓解可能的内存泄漏
/// </summary>
public class MemoryWatchDogService(ILogger<MemoryWatchDogService> logger) : BackgroundService
{
    private ILogger<MemoryWatchDogService> Logger { get; } = logger;

    /// <summary>
    /// 软上限：达到后会尝试触发一次 GC/LOH 压缩（带冷却）。
    /// </summary>
    public static readonly long SoftMemoryLimitBytes = 1200000000;

    /// <summary>
    /// 指示应用程序所允许使用的最大内存使用量(Bytes)，默认值为1.5GB
    /// </summary>
    public static readonly long MemoryLimitBytes = 1500000000;

    /// <summary>
    /// 需连续超出硬上限的次数阈值。
    /// </summary>
    public static readonly int HardLimitConsecutiveHits = 2;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan GcCooldown = TimeSpan.FromMinutes(2);
    private int _hardLimitHitCount = 0;
    private DateTimeOffset _lastGcAttemptAt = DateTimeOffset.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                CheckMemoryAndRestartIfNeeded();
            }
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
    }

    /// <summary>
    /// 获取主应用程序所使用的内存占用大小
    /// </summary>
    /// <remarks>由于<see href="https://github.com/dotnet/runtime/issues/105665">此问题</see>，macOS平台上会返回<see cref="Process.WorkingSet64"/>字段，其他平台会返回<see cref="Process.PrivateMemorySize64"/>字段</remarks>
    /// <returns>主应用程序所使用的内存占用大小(Bytes)</returns>
    public static long GetMemoryUsage()
    {
        return OperatingSystem.IsMacOS()
            ? Process.GetCurrentProcess().WorkingSet64
            : Process.GetCurrentProcess().PrivateMemorySize64;
    }

    private void CheckMemoryAndRestartIfNeeded()
    {
        var now = DateTimeOffset.UtcNow;
        var size = GetMemoryUsage();
        Logger.LogInformation("当前内存使用: {}", Helpers.StorageSizeHelper.FormatSize((ulong)size)+$"({size} Bytes)");

        if (size >= SoftMemoryLimitBytes && now - _lastGcAttemptAt >= GcCooldown)
        {
            _lastGcAttemptAt = now;
            TryReduceMemoryPressure();
            size = GetMemoryUsage();
            Logger.LogInformation("GC 后内存使用: {}", Helpers.StorageSizeHelper.FormatSize((ulong)size)+$"({size} Bytes)");
        }

        if (size < MemoryLimitBytes)
        {
            _hardLimitHitCount = 0;
            return;
        }

        _hardLimitHitCount++;
        if (_hardLimitHitCount < HardLimitConsecutiveHits)
        {
            Logger.LogWarning("超过内存上限（连续 {Hit}/{Need} 次）。", _hardLimitHitCount, HardLimitConsecutiveHits);
            return;
        }

        Logger.LogCritical("达到内存使用上限！ {} / {}", Helpers.StorageSizeHelper.FormatSize((ulong)size)+$"({size} Bytes)", Helpers.StorageSizeHelper.FormatSize((ulong)MemoryLimitBytes)+$"({MemoryLimitBytes} Bytes)");
        AppBase.Current.Restart(["-q", "-m", "-psmk"]); // 静默，等待本会话退出，指示本会话因 MLE 而结束
    }

    private void TryReduceMemoryPressure()
    {
        try
        {
            System.Runtime.GCSettings.LargeObjectHeapCompactionMode =
                System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "尝试降低内存占用时出现异常。");
        }
    }
}
