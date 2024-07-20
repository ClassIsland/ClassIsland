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
    new Option<bool>(["-disableManagement", "-dm"], "在本次会话禁用集控。")
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