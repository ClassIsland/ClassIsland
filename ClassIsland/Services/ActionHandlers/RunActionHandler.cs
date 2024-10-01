using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models.Actions;

using Microsoft.Extensions.Hosting;

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Encodings.Web;
using System.Windows.Controls;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Windows.Shapes;
using Path = System.IO.Path;
using WebSocketSharp;
namespace ClassIsland.Services.ActionHandlers;

internal class RunActionHandler
{
    public ILogger<RunActionHandler> Logger { get; }

    internal RunActionHandler(IActionService ActionService, ILogger<RunActionHandler> logger)
    {
        Logger = logger;
        ActionService.RegisterActionHandler("classisland.os.run",
            (s, g) => Run(((RunActionSettings)s!).Value));
    }

    private bool Run(string value)
    {
        // 文件(夹)
        if (File.Exists(value) || Directory.Exists(value))
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = Path.GetFullPath(value), // TODO: 允许参数
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开文件(夹)失败。");
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
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };
            process.Start();
            Logger.LogTrace("cmd 命令输出：{}", process.StandardOutput.ReadToEnd());

            if (string.IsNullOrEmpty(process.StandardError.ReadToEnd()))
                return true;
        } catch { }

        // 网页
        if (value.Contains('.'))
        {
            var path = value;
            if (!Regex.IsMatch(value, @"^https?:\/\/", RegexOptions.IgnoreCase))
            {
                path = "http://" + path;
            }
            if (Uri.TryCreate(path, UriKind.Absolute, out Uri uri))
            {
                Process.Start("explorer.exe", uri.AbsoluteUri);
                return true;
            }
        }

        return false;
    }
}
