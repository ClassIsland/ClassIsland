using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Enums;
using ClassIsland.Models.ActionSettings;
using Microsoft.Extensions.Logging;
namespace ClassIsland.Services.Automation.Actions;

/// <summary>
/// "运行"行动的行动提供方。
/// </summary>
[ActionInfo("classisland.os.run", "运行", "\uec2e", false)]
public sealed class RunAction : ActionBase<RunActionSettings>
{
    protected override async Task OnInvoke()
    {
        switch (Settings.RunType)
        {
            case RunActionRunType.Application:
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = Settings.Value,
                    Arguments = Settings.Args,
                    UseShellExecute = true
                });
                break;
            }
            case RunActionRunType.File:
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = Settings.Value,
                    UseShellExecute = true
                });
                break;
            }
            case RunActionRunType.Folder:
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = Settings.Value,
                    UseShellExecute = true
                });
                break;
            }
            case RunActionRunType.Url:
            {
                var path = Settings.Value;
                if (!path.Contains("://"))
                    path = "http://" + path;
                Process.Start("explorer.exe", path);
                break;
            }
            case RunActionRunType.Command:
            {
                await RunCommandAsync(Settings.Value, CancellationToken);
                break;
            }
            default:
                throw new ArgumentException();
        }
    }

    static async Task RunCommandAsync(string command, CancellationToken? cancellationToken = null)
    {
        using var process = new Process();
        process.EnableRaisingEvents = true;
        process.StartInfo = new ProcessStartInfo
        {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash",
            Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? $"/c \"{command}\""
                : $"-c \"{command.Replace("\"", "\\\"")}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (!process.Start())
            throw new InvalidOperationException("无法启动进程");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await (cancellationToken.HasValue
                ? process.WaitForExitAsync(cancellationToken.Value)
                : process.WaitForExitAsync())
            .ConfigureAwait(false);

        var stdout = await outputTask.ConfigureAwait(false);
        var stderr = await errorTask.ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"命令执行失败 (退出代码: {process.ExitCode})\n标准输出：{stdout}\n错误输出：{stderr}");
        }
    }
}