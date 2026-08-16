using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core;
using ClassIsland.Core.Helpers;
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
    /// 指示应用程序所允许使用的最大内存使用量(Bytes)，默认值为1.5GB
    /// </summary>
    public static readonly long MemoryLimitBytes = 1500000000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                CheckMemoryAndRestartIfNeeded();
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// 获取主应用程序所使用的内存占用大小
    /// </summary>
    /// <remarks>
    /// iOS/iPadOS 不支持通过 <see cref="Process"/> 获取进程内存信息；当平台不支持该 API 时，
    /// 返回托管堆内存用量。
    /// 由于<see href="https://github.com/dotnet/runtime/issues/105665">此问题</see>，macOS 平台上会返回
    /// <see cref="Process.WorkingSet64"/> 字段，其他平台会返回 <see cref="Process.PrivateMemorySize64"/> 字段。
    /// </remarks>
    /// <returns>主应用程序所使用的内存占用大小(Bytes)</returns>
    public static long GetMemoryUsage()
    {
        if (OperatingSystem.IsIOS())
        {
            return GC.GetTotalMemory(false);
        }

        try
        {
            return GetProcessMemoryUsage();
        }
        catch (PlatformNotSupportedException)
        {
            return GC.GetTotalMemory(false);
        }
    }

    [UnsupportedOSPlatform("ios")]
    private static long GetProcessMemoryUsage()
    {
        var process = Process.GetCurrentProcess();
        return OperatingSystem.IsMacOS()
            ? process.WorkingSet64
            : process.PrivateMemorySize64;
    }

    private void CheckMemoryAndRestartIfNeeded()
    {
        if (PlatformHelper.IsMobile)
        {
            return;
        }
        var size = GetMemoryUsage();
        var formatted = Helpers.StorageSizeHelper.FormatSize((ulong)size);
        Logger.LogInformation("当前内存使用: {} ({} Bytes)", formatted, size);
        if (size < MemoryLimitBytes) 
            return;
        var limitFormatted = Helpers.StorageSizeHelper.FormatSize((ulong)MemoryLimitBytes);
        Logger.LogCritical("达到内存使用上限！ {} ({} Bytes) / {} ({} Bytes)",
            formatted, size,
            limitFormatted, MemoryLimitBytes);

        AppBase.Current.Restart(["-q", "-m", "-psmk"]); // 静默，等待本会话退出，指示本会话因MLE而结束
    }
}
