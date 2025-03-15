namespace ClassIsland.Shared.Models;

/// <summary>
/// 代表一个配置错误信息。
/// </summary>
public class ConfigError
{
    /// <summary>
    /// 配置文件路径
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// 错误等级
    /// </summary>
    /// <value>
    /// 0 - 已自动从备份恢复 <br/>
    /// 1 - 无法恢复的错误
    /// </value>
    public int Level { get; }

    /// <summary>
    /// 是否是关键错误
    /// </summary>
    public bool Critical { get; }

    /// <summary>
    /// 异常信息
    /// </summary>
    public Exception Exception { get; }

    internal ConfigError(string path, int level, bool critical, Exception exception)
    {
        Path = path;
        Level = level;
        Critical = critical;
        Exception = exception;
    }
}