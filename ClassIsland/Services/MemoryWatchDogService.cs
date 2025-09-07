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
                var info = new TaskBasicInfo();
                var size = Marshal.SizeOf(typeof(TaskBasicInfo));
                var result = task_info(mach_task_self(), TaskFlavorBasicInfo, ref info, ref size);

                if (result == 0)
                {
                    return info.ResidentSize;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "无法通过 task_info 获取内存使用情况");
            }
        }
        else
        {
            return Process.GetCurrentProcess().PrivateMemorySize64;
        }

        return 0;
    }

    private const int TaskFlavorBasicInfo = 20;

    [StructLayout(LayoutKind.Sequential)]
    private struct TaskBasicInfo
    {
        public int VirtualSize;
        public int ResidentSize;
        public int ResidentSizeMax;
        public int UserTime;
        public int SystemTime;
        public int Policy;
        public int SuspendCount;
    }

    [DllImport("libc")]
    private static extern int task_info(IntPtr task, int flavor, ref TaskBasicInfo info, ref int size);

    [DllImport("libc")]
    private static extern IntPtr mach_task_self();

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