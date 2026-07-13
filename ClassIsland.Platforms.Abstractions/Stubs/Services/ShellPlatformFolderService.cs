using System.Diagnostics;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Abstraction.Stubs.Services;

/// <summary>
/// 使用系统 shell 显示目录的默认实现。
/// </summary>
public sealed class ShellPlatformFolderService : IPlatformFolderService
{
    /// <inheritdoc />
    public Task<bool> OpenFolderAsync(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        var fullPath = Path.GetFullPath(folderPath);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"要打开的目录不存在：{fullPath}");
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = fullPath,
            UseShellExecute = true
        });
        return Task.FromResult(true);
    }
}
