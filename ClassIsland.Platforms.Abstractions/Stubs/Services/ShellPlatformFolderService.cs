using System.Diagnostics;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Abstraction.Stubs.Services;

/// <summary>
/// 使用系统 shell 显示目录的默认实现。
/// </summary>
public sealed class ShellPlatformFolderService : IPlatformFolderService
{
    private readonly Action<ProcessStartInfo> _startProcess;

    /// <summary>
    /// 初始化默认的系统 shell 目录服务。
    /// </summary>
    public ShellPlatformFolderService() : this(startInfo => Process.Start(startInfo))
    {
    }

    internal ShellPlatformFolderService(Action<ProcessStartInfo> startProcess)
    {
        ArgumentNullException.ThrowIfNull(startProcess);
        _startProcess = startProcess;
    }

    /// <inheritdoc />
    public Task<bool> OpenFolderAsync(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        var fullPath = Path.GetFullPath(folderPath);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"要打开的目录不存在：{fullPath}");
        }

        _startProcess(new ProcessStartInfo
        {
            FileName = fullPath,
            UseShellExecute = true
        });
        return Task.FromResult(true);
    }
}
