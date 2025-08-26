using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using ClassIsland.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        if (OperatingSystem.IsWindows())
        {
            return Process.GetCurrentProcess().PrivateMemorySize64;
        }
        else if (OperatingSystem.IsLinux())
        {
            try
            {
                var statmPath = "/proc/self/statm";
                if (File.Exists(statmPath))
                {
                    var contents = File.ReadAllText(statmPath);
                    var memoryPages = contents.Split(' ')[0];
                    var pageSize = Environment.SystemPageSize;
                    return long.Parse(memoryPages) * pageSize;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "无法读取 /proc/self/statm 获取内存使用情况");
            }
        }
        else if (OperatingSystem.IsMacOS())
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sysctl",
                    Arguments = "hw.memsize",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    var memorySize = output.Split(':')[1].Trim();
                    return long.Parse(memorySize);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "无法通过 sysctl 获取内存使用情况");
            }
        }
        return 0;
    }

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