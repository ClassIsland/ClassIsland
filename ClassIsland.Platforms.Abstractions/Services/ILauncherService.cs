namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 启动器服务。
/// </summary>
public interface ILauncherService
{
    /// <summary>
    /// 启动一个目录。
    /// </summary>
    /// <param name="path">要启动的目录</param>
    public Task LaunchPath(string path);

    /// <summary>
    /// 启动一个外部 URL
    /// </summary>
    /// <param name="url">URL</param>
    public Task LaunchUrl(string url);
}