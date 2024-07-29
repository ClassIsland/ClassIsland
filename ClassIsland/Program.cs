using ClassIsland.Models;
using System;
using System.Collections;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine;
using System.Diagnostics;
using System.Threading;
using ClassIsland;
using ClassIsland.Shared.IPC.Protobuf.Client;
using ClassIsland.Shared.IPC;
using Sentry;
using Sentry.Profiling;

Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

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
    new Option<bool>(["--enableSentryDebug", "-esd"], "启用 Sentry 调试")
};
command.Handler = CommandHandler.Create((ApplicationCommand c) => { App.ApplicationCommand = c; });
command.Invoke(args);

var mutex = new Mutex(true, "ClassIsland.Lock", out var createNew);

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
            ProcessUriNavigation();
        }
    }
}

SentrySdk.Init(options =>
{
    // A Sentry Data Source Name (DSN) is required.
    // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
    // You can set it in the SENTRY_DSN environment variable, or you can set it in code here.
    options.Dsn = "http://16f66314173eb09592b08a5ee80f7352@110.40.26.143:9000/2";
    // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
    // This might be helpful, or might interfere with the normal operation of your application.
    // We enable it here for demonstration purposes when first trying Sentry.
    // You shouldn't do this in your applications unless you're troubleshooting issues with Sentry.
    options.Debug = App.ApplicationCommand.EnableSentryDebug;
    // This option is recommended. It enables Sentry's "Release Health" feature.
    options.AutoSessionTracking = true;
    options.Release = App.AppVersion;
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
        options.TracesSampleRate = 0.1;
        // options.ProfilesSampleRate = 0.016;
    }
    options.AutoSessionTracking = true;
    options.ExperimentalMetrics = new ExperimentalMetricsOptions { EnableCodeLocations = true };
});
var app = new App()
{
    Mutex = mutex,
    IsMutexCreateNew = createNew
};
app.InitializeComponent();
app.Run();
return;

static void ProcessUriNavigation()
{
    try
    {
        var client = new IpcClient();
        var uriSc =
            new ClassIsland.Shared.IPC.Protobuf.Service.UriNavigationService.UriNavigationServiceClient(
                client.Channel);
        uriSc.Navigate(new UriNavigationScReq()
        {
            Uri = App.ApplicationCommand.Uri
        });
        Environment.Exit(0);
    }
    catch
    {
        // ignored
    }
}