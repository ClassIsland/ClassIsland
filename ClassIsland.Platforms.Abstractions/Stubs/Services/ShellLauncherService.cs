using System.Diagnostics;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Abstraction.Stubs.Services;

/// <summary>
/// 使用系统 shell 打开目录和外部 URL 的默认启动器服务。
/// </summary>
public sealed class ShellLauncherService : ILauncherService
{
    private readonly Action<ProcessStartInfo> _startProcess;

    /// <summary>
    /// 初始化默认的系统 shell 启动器服务。
    /// </summary>
    public ShellLauncherService() : this(startInfo => Process.Start(startInfo))
    {
    }

    internal ShellLauncherService(Action<ProcessStartInfo> startProcess)
    {
        ArgumentNullException.ThrowIfNull(startProcess);
        _startProcess = startProcess;
    }

    /// <inheritdoc />
    public Task LaunchPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"要打开的目录不存在：{fullPath}");
        }

        _startProcess(new ProcessStartInfo
        {
            FileName = fullPath,
            UseShellExecute = true
        });
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LaunchUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("只能打开绝对 URL。", nameof(url));
        }

        _startProcess(new ProcessStartInfo
        {
            FileName = uri.AbsoluteUri,
            UseShellExecute = true
        });
        return Task.CompletedTask;
    }
}
