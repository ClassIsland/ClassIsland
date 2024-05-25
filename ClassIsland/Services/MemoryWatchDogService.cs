using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

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

    private void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        var size = Process.GetCurrentProcess().PrivateMemorySize64;
        //Console.WriteLine(size);
        Logger.LogInformation("当前内存使用: {}", size);
        if (size < MemoryLimitBytes) 
            return;
        Logger.LogCritical("达到内存使用上限！ {} / {}", size, MemoryLimitBytes);
        //var startInfo = Process.GetCurrentProcess().StartInfo;
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
        //Process.Start(startInfo);
        Application.Current.Dispatcher.Invoke(() =>
        {
            App.GetService<SettingsService>().SaveSettings();
            App.GetService<ProfileService>().SaveProfile();
            Application.Current.Shutdown(0);
        });
    }
}