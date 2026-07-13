using System.Text.Json;

namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 通过一次性文件在应用进程重新启动之间保留启动参数。
/// </summary>
internal sealed class PendingLaunchArgumentsStore
{
    private readonly string _storePath;
    private readonly Action<string> _deleteFile;

    public PendingLaunchArgumentsStore(
        string storePath,
        Action<string>? deleteFile = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storePath);

        _storePath = storePath;
        _deleteFile = deleteFile ?? File.Delete;
    }

    public void Save(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var directory = Path.GetDirectoryName(_storePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = _storePath + ".tmp";
        try
        {
            File.WriteAllText(
                temporaryPath,
                JsonSerializer.Serialize(arguments));
            File.Move(temporaryPath, _storePath, true);
        }
        catch
        {
            TryDelete(temporaryPath);
            throw;
        }
    }

    public string[] Consume()
    {
        if (!File.Exists(_storePath))
        {
            return [];
        }

        string[] arguments;
        try
        {
            arguments =
                JsonSerializer.Deserialize<string[]>(File.ReadAllText(_storePath)) ?? [];
        }
        catch
        {
            // 损坏的参数不能让应用陷入启动循环。
            TryDelete(_storePath);
            return [];
        }

        // 删除失败时仍允许启动；下次启动可能再次收到参数，比阻断冷启动更可控。
        TryDelete(_storePath);
        return arguments;
    }

    private void TryDelete(string path)
    {
        try
        {
            _deleteFile(path);
        }
        catch
        {
            // 清理失败不得覆盖读取或写入阶段的真实结果。
        }
    }
}
