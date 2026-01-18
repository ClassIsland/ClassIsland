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

public class MemoryWatchDogService(ILogger<MemoryWatchDogService> logger) : BackgroundService
{
    private ILogger<MemoryWatchDogService> Logger { get; } = logger;

    private Timer Timer { get; } = new()
    {
        Interval = 60000
    };

    public static readonly long MemoryLimitBytes = 1500000000; // 1.5GB


    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Timer.Elapsed += TimerOnElapsed;
        Timer.Start();
        return Task.CompletedTask;
    }
    /// <summary>
    /// 获取主应用程序所使用的内存占用大小
    /// </summary>
    /// <remarks>macOS平台上会返回<see cref="Process.WorkingSet64"/>字段，其他平台会返回<see cref="Process.PrivateMemorySize64"/>字段</remarks>
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
        AppBase.Current.Restart(["-q", "-m", "-psmk"]);
    }
}