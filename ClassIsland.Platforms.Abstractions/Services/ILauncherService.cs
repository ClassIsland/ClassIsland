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
    /// 打开或预览文件；默认沿用现有平台的路径启动行为。
    /// </summary>
    /// <param name="path">文件路径或平台引用</param>
    public Task LaunchFile(string path) => LaunchPath(path);

    /// <summary>
    /// 启动一个外部 URL
    /// </summary>
    /// <param name="url">URL</param>
    public Task LaunchUrl(string url);

    /// <summary>
    /// 通过应用提供的链接打开应用；默认沿用平台的 URL 启动行为。
    /// </summary>
    /// <param name="url">应用链接</param>
    public Task LaunchAppLink(string url) => LaunchUrl(url);
}
