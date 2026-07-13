namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 提供使用系统文件管理器显示指定目录的能力。
/// </summary>
public interface IPlatformFolderService
{
    /// <summary>
    /// 使用系统文件管理器显示指定目录。
    /// </summary>
    /// <param name="folderPath">要显示的目录路径。</param>
    /// <returns>系统文件管理器是否已成功打开。</returns>
    Task<bool> OpenFolderAsync(string folderPath);
}
