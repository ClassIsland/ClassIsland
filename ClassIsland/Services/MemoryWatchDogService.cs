using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using ClassIsland.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

using Timer = System.Timers.Timer;

namespace ClassIsland.Services;

/// <summary>
/// 应用程序内存监视服务，用于在内存使用超出<see cref="MemoryLimitBytes"/>时重启应用程序，缓解可能的内存泄漏
/// </summary>
public class MemoryWatchDogService(ILogger<MemoryWatchDogService> logger) : BackgroundService
{
    private ILogger<MemoryWatchDogService> Logger { get; } = logger;

    private Timer Timer { get; } = new()
    {
        Interval = 60000 // 60s
    };

    /// <summary>
    /// 指示应用程序所允许使用的最大内存使用量(Bytes)，默认值为1.5GB
    /// </summary>
    public static readonly long MemoryLimitBytes = 1500000000;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Timer.Elapsed += TimerOnElapsed;
        Timer.Start();
        return Task.CompletedTask;
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

    private void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        var size = GetMemoryUsage();
        Logger.LogInformation("当前内存使用: {}", Helpers.StorageSizeHelper.FormatSize((ulong)size)+$"({size} Bytes)");
        if (size < MemoryLimitBytes) 
            return;
        Logger.LogCritical("达到内存使用上限！ {} / {}", Helpers.StorageSizeHelper.FormatSize((ulong)size)+$"({size} Bytes)", Helpers.StorageSizeHelper.FormatSize((ulong)MemoryLimitBytes)+$"({MemoryLimitBytes} Bytes)");
        AppBase.Current.Restart(["-q", "-m", "-psmk"]); // 静默，等待本会话退出，指示本会话因MLE而结束
    }
}