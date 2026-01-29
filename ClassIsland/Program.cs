using ClassIsland.Models;
using System;
using System.Collections.Generic;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using ClassIsland;
using ClassIsland.Core;
using ClassIsland.Core.Converters;
using ClassIsland.Core.Enums;
using ClassIsland.Extensions;
using ClassIsland.Services;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.IPC;
using ClassIsland.Shared.IPC.Abstractions.Services;
using dotnetCampus.Ipc.CompilerServices.GeneratedProxies;
using HotAvalonia;
using Sentry;
using System.Diagnostics;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Services;
using ClassIsland.Shared.JsonConverters;

namespace ClassIsland;

public static class Program
{
    [STAThread]
    public static Func<App> AppEntry(string[] args, Action? postInit = null)
    {
        AppDomain.CurrentDomain.UnhandledException += DiagnosticService.ProcessDomainUnhandledException;
        AppBase.CurrentLifetime = ApplicationLifetime.EarlyLoading;
        
        ConfigureFileHelper.SerializerOptions.Converters.Add(new ColorHexJsonConverter());
        ConfigureFileHelper.SerializerOptions.Converters.Add(new GuidEmptyFallbackConverter());

        var command = new RootCommand
        {
            new Option<string>(["--updateReplaceTarget", "-urt"], "更新时要替换的文件"),
            new Option<string>(["--updateDeleteTarget", "-udt"], "更新完成要删除的文件"),
            new Option<string>(["--uri"], "启动时要导航到的Uri"),
            new Option<bool>(["--waitMutex", "-m"], "重复启动应用时，等待上一个实例退出而非直接退出应用。"),
            new Option<bool>(["--quiet", "-q"], "静默启动，启动时不显示Splash，并且启动后10秒内不显示任何通知。"),
            new Option<bool>(["-prevSessionMemoryKilled", "-psmk"], "上个会话因MLE结束。"),
            new Option<bool>(["-disableManagement", "-dm"], "在本次会话禁用集控。"),
            new Option<string>(["-externalPluginPath", "-epp"], "外部插件路径"),
            new Option<bool>(["--enableSentryDebug", "-esd"], "启用 Sentry 调试"),
            new Option<bool>(["--verbose", "-v"], "启用详细输出"),
            new Option<bool>(["--showOssWatermark", "-ossw"], "显示开源地址水印"),
            new Option<bool>(["--recovery", "-r"], "启动时进入恢复模式"),
            new Option<bool>(["--diagnostic", "-d"], "启用诊断模式"),
            new Option<bool>(["--safe", "-s"], "启用安全模式"),
            new Option<bool>(["--skip-oobe", "-so"], "跳过 OOBE 启动"),
            new Option<string>(["--importV1"], "指定要导入的 ClassIsland 1.7 配置目录"),
            new Option<string>(["--importEntries"], "指定要导入的 ClassIsland 1.7 配置信息"),
            new Option<bool>(["--importComplete"], "启动时显示导入完成窗口"),
            new Option<bool>(["--importV1Complete"], "从 ClassIsland 1 导入成功"),
        };
        command.Handler = CommandHandler.Create((ApplicationCommand c) => { App.ApplicationCommand = c; });
        command.Invoke(args);
        
        GlobalStorageService.InitializeGlobalStorage();

        if (App.ApplicationCommand.Diagnostic)
        {
            // TODO: 实现 AllocConsole
            // AllocConsole();
        }

        // 修复特定情况下无法正常 DPI 缩放的问题 https://github.com/ClassIsland/ClassIsland/issues/1580
        if (OperatingSystem.IsLinux() && GlobalStorageService.GetValue("IgnoreQtScaling") == "1")
        {
            Environment.SetEnvironmentVariable("QT_SCREEN_SCALE_FACTORS", null);
            Environment.SetEnvironmentVariable("QT_SCALE_FACTORS", null);
        }

        var mutex = new Mutex(true, "Global\\ClassIsland.Lock", out var createNew);

        if (!createNew)
        {
            if (App.ApplicationCommand.WaitMutex)
            {
                try
                {
                    mutex?.WaitOne();
                }
                catch
                {
                    // ignored
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(App.ApplicationCommand.Uri))
                {
                    ProcessUriNavigationAsync().Wait();
                }
            }
        }

        var sentryEnabled = GlobalStorageService.GetValue("IsSentryEnabled") is "1" or null;
        if (sentryEnabled )
        {
            SentrySdk.Init(ConfigureSentry);
        }
        try {
            if (Environment.GetEnvironmentVariable("ClassIsland_ProcessPriority") is { } priorityStr && uint.TryParse(priorityStr, out uint priority))
            {
                SetProcessPriority(priority);
            }
            else SetProcessPriority(2); //If not set or invalid, default to Normal priority (2).
        }
        catch
        {
            // ignore
        }

        return () => new App()
        {
            Mutex = mutex,
            IsMutexCreateNew = createNew,
            IsSentryEnabled = sentryEnabled
        };
    }
    
    /// <summary>
    /// 用于在发现另一个实例正在运行时，将启动 URI 通过 IPC 发送给已运行实例并退出当前进程。
    /// 此方法在启动参数包含 URI 时被调用以支持单实例的 URI 导航。
    /// </summary>
    private static async Task ProcessUriNavigationAsync()
    {
        try
        {
            var client = new IpcClient();
            await client.Connect();
            var uriSc = client.Provider.CreateIpcProxy<IPublicUriNavigationService>(client.PeerProxy!);
            uriSc.Navigate(new Uri(App.ApplicationCommand.Uri));
            Environment.Exit(0);
        }
        catch
        {
            // ignored
        }
    }
    
    /// <summary>
    /// 配置 Sentry SDK 的运行时选项。
    /// 在启用 Sentry 时由 <see cref="AppEntry"/> 调用以初始化全局监控参数。
    /// </summary>
    private static void ConfigureSentry(SentryOptions options)
    {
        // A Sentry Data Source Name (DSN) is required.
        // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
        // You can set it in the SENTRY_DSN environment variable, or you can set it in code here.
        options.Dsn = "https://16f66314173eb09592b08a5ee80f7352@todayeatsentry.classisland.tech:21815/2";
        // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
        // This might be helpful, or might interfere with the normal operation of your application.
        // We enable it here for demonstration purposes when first trying Sentry.
        // You shouldn't do this in your applications unless you're troubleshooting issues with Sentry.
        options.Debug = App.ApplicationCommand.EnableSentryDebug;
        // This option is recommended. It enables Sentry's "Release Health" feature.
        options.AutoSessionTracking = true;
        options.Release = App.AppVersion;
        options.SendClientReports = false;
        // Enabling this option is recommended for client applications only. It ensures all threads use the same global scope.
        options.IsGlobalModeEnabled = true;
        // Example sample rate for your transactions: captures 10% of transactions
        if (App.ApplicationCommand.EnableSentryDebug)
        {
            options.TracesSampleRate = 1.0;
            // options.ProfilesSampleRate = 1.0;
        }
        else
        {
            options.TracesSampleRate = 0.05;
            // options.ProfilesSampleRate = 0.016;
        }

        options.ExperimentalMetrics = new ExperimentalMetricsOptions { EnableCodeLocations = true };
    }
    
    /// <summary>
    /// 设置应用程序的 <see cref="ProcessPriorityClass"/>。
    /// 无效值将回退到 <see cref="ProcessPriorityClass.Normal"/>。
    /// </summary>
    static void SetProcessPriority(uint priority)
    {
        Process.GetCurrentProcess().PriorityClass = priority switch
        {
            0 => ProcessPriorityClass.Idle,
            1 => ProcessPriorityClass.BelowNormal,
            2 => ProcessPriorityClass.Normal,
            3 => ProcessPriorityClass.AboveNormal,
            4 => ProcessPriorityClass.High,
            5 => ProcessPriorityClass.RealTime,
            _ => ProcessPriorityClass.Normal,
        };
    }
}

