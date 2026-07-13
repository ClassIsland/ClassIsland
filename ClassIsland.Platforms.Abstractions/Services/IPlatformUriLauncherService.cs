namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 提供使用系统默认应用打开外部 URI 的能力。
/// </summary>
public interface IPlatformUriLauncherService
{
    /// <summary>
    /// 使用系统默认应用打开外部 URI。
    /// </summary>
    /// <param name="uri">要打开的绝对 URI。</param>
    /// <returns>系统是否接受了打开请求。</returns>
    Task<bool> OpenUriAsync(Uri uri);
}
