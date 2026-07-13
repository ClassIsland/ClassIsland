using System.Diagnostics;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Abstraction.Stubs.Services;

/// <summary>
/// 使用系统 shell 打开外部 URI 的默认实现。
/// </summary>
public sealed class ShellPlatformUriLauncherService : IPlatformUriLauncherService
{
    private readonly Action<ProcessStartInfo> _startProcess;

    /// <summary>
    /// 初始化默认的系统 shell URI 启动服务。
    /// </summary>
    public ShellPlatformUriLauncherService() : this(startInfo => Process.Start(startInfo))
    {
    }

    internal ShellPlatformUriLauncherService(Action<ProcessStartInfo> startProcess)
    {
        ArgumentNullException.ThrowIfNull(startProcess);
        _startProcess = startProcess;
    }

    /// <inheritdoc />
    public Task<bool> OpenUriAsync(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri)
        {
            throw new ArgumentException("只能打开绝对 URI。", nameof(uri));
        }

        _startProcess(new ProcessStartInfo
        {
            FileName = uri.AbsoluteUri,
            UseShellExecute = true
        });
        return Task.FromResult(true);
    }
}
