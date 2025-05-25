using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models.Actions;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using System;
namespace ClassIsland.Services.ActionHandlers;

public class RunActionHandler : IHostedService
{
    public ILogger<RunActionHandler> Logger { get; }

    public RunActionHandler(IActionService ActionService, ILogger<RunActionHandler> logger)
    {
        Logger = logger;
        ActionService.RegisterActionHandler("classisland.os.run",
            (s, _) => Run((s as RunActionSettings).Value,
                          (s as RunActionSettings).Args));
    }

    private void Run(string value, string args)
    {
        // 文件(夹)
        {
            var path = value.Replace('"'.ToString(), "");
            if (File.Exists(path) || Directory.Exists(path))
            {
                try
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = value,
                        Arguments = args,
                        UseShellExecute = true
                    });
                    return;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "打开文件(夹)失败。");
                }
            }
        }

        // cmd 命令
        try {
            Process process = new()
            {
                StartInfo = new()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {value}",
                    RedirectStandardError = true
                }
            };
            process.Start();

            if (string.IsNullOrEmpty(process.StandardError.ReadToEnd()))
                return;
        }
        catch { }

        // 网页
        {
            var path = value;
            if (value.Contains('.') && !value.Contains("://"))
            {
                path = "http://" + path;
            }
            if (Uri.TryCreate(path, UriKind.Absolute, out Uri uri))
            {
                Process.Start("explorer.exe", uri.AbsoluteUri);
                return;
            }
        }

        throw new InvalidOperationException($"未能运行“{value}”：未匹配到合适的运行方式。");
    }

    public async Task StartAsync(CancellationToken _) { }
    public async Task StopAsync(CancellationToken _) { }
}
