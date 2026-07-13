namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 启动器服务。
/// </summary>
public interface ILauncherService
{
    /// <summary>
    /// 使用系统文件管理器打开目录。
    /// </summary>
    /// <param name="path">要打开的目录。</param>
    Task LaunchPath(string path);

    /// <summary>
    /// 使用系统默认应用打开外部 URL。
    /// </summary>
    /// <param name="url">要打开的绝对 URL。</param>
    Task LaunchUrl(string url);
}
