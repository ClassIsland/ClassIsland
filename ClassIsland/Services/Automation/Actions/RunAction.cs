using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.Actions;
using static ClassIsland.Models.Actions.RunActionSettings.RunActionRunType;
namespace ClassIsland.Services.Automation.Actions;

[ActionInfo("classisland.os.run", "打开 / 运行", "\uec2e", addDefaultToMenu:false)]
[ContributorInfo("@dryice @lrs @baiyao")]
public class RunAction : ActionBase<RunActionSettings>
{
    protected override async Task OnInvoke()
    {
        await base.OnInvoke();
        switch (Settings.RunType)
        {
            case Application:
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Settings.Value,
                    Arguments = Settings.Args,
                    UseShellExecute = true
                });
                break;
            }
            case File:
            case Folder:
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Settings.Value,
                    UseShellExecute = true
                });
                break;
            }
            case Url:
            {
                var path = Settings.Value;
                if (!string.IsNullOrWhiteSpace(path) && !path.Contains(':') && !path.StartsWith('\\'))
                    path = "https://" + path;
                App.GetService<IUriNavigationService>().Navigate(new(path));

                break;
            }
            case Command:
            {
                await RunCommandAsync(Settings.Value, InterruptCancellationToken);
                break;
            }
            default:
                throw new NotSupportedException();
        }
    }


    const int MaxLength = 2000;
    static async Task RunCommandAsync(string command, CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.EnableRaisingEvents = true;
        process.StartInfo = new ProcessStartInfo(
            OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash",
            OperatingSystem.IsWindows()
                ? $"/c \"{command}\""
                : $"-c \"{command.Replace("\"", "\\\"")}\"")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var stdoutBuilder = new StringBuilder(capacity: MaxLength);
        var stderrBuilder = new StringBuilder(capacity: MaxLength);

        process.OutputDataReceived += (s, e) => OnDataReceived(e, stdoutBuilder);
        process.ErrorDataReceived += (s, e) => OnDataReceived(e, stderrBuilder);

        if (!process.Start())
            throw new InvalidOperationException("无法启动进程。");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            throw new OperationCanceledException(
                $"命令执行被中断。" + Environment.NewLine +
                $"标准输出：{(stdoutBuilder.Length == 0 ? "(空)" : stdoutBuilder)}" + Environment.NewLine +
                $"错误输出：{(stderrBuilder.Length == 0 ? "(空)" : stderrBuilder)}");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"命令执行失败 (退出代码: {process.ExitCode})。" +Environment.NewLine+
                $"标准输出：{(stdoutBuilder.Length == 0 ? "(空)" : stdoutBuilder)}" + Environment.NewLine +
                $"错误输出：{(stderrBuilder.Length == 0 ? "(空)" : stderrBuilder)}");
        }

        return;

        static void OnDataReceived(DataReceivedEventArgs e, StringBuilder sb)
        {
            if (e.Data is null || sb.Length >= MaxLength)
                return;
            var remaining = MaxLength - sb.Length;
            if (e.Data.Length <= remaining)
            {
                if (sb.Length > 0)
                    sb.AppendLine();
                sb.Append(e.Data);
            }
            else
            {
                sb.Append(e.Data, 0, remaining);
                sb.AppendLine();
                sb.Append("…（已截断）");
            }
        }
    }
}
