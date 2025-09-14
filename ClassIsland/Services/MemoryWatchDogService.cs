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

    private long GetMemoryUsage()
    {
        if (OperatingSystem.IsMacOS())
        {
            try
            {
                var mib = new int[2] { CTL_HW, HW_MEMSIZE };
                long physicalMemory = 0;
                var length = Marshal.SizeOf(typeof(long));

                if (sysctl(mib, 2, ref physicalMemory, ref length, IntPtr.Zero, 0) == 0)
                {
                    return physicalMemory;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "无法通过 sysctl 获取内存使用情况");
            }
        }
        else
        {
            return Process.GetCurrentProcess().PrivateMemorySize64;
        }

        return 0;
    }

    private const int CTL_HW = 6;
    private const int HW_MEMSIZE = 24;

    [DllImport("libc", SetLastError = true)]
    private static extern int sysctl(
        int[] name,
        uint namelen,
        ref long oldp,
        ref int oldlenp,
        IntPtr newp,
        uint newlen);

    private void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        var size = GetMemoryUsage();
        Logger.LogInformation("当前内存使用: {}", Helpers.StorageSizeHelper.FormatSize((ulong)size)+$"({size} Bytes)");
        if (size < MemoryLimitBytes) 
            return;
        Logger.LogCritical("达到内存使用上限！ {} / {}", Helpers.StorageSizeHelper.FormatSize((ulong)size)+$"({size} Bytes)", Helpers.StorageSizeHelper.FormatSize((ulong)MemoryLimitBytes)+$"({MemoryLimitBytes} Bytes)");
        var path = Environment.ProcessPath;
        if (path != null)
        {
            var replaced = path.Replace(".dll", ".exe");
            var startInfo = new ProcessStartInfo(replaced)
            {
                ArgumentList =
                {
                    "-q", "-m", "-psmk"
                }
            };
            Process.Start(startInfo);
        }
        AppBase.Current.Stop();
    }
}