namespace ClassIsland.Core.Helpers;

/// <summary>
/// 路径通用助手方法
/// </summary>
public static class PathHelpers
{
    /// <summary>
    /// 判断路径是否安全。
    /// </summary>
    /// <param name="baseDirectory">基础路径</param>
    /// <param name="userInputPath">输入路径</param>
    /// <returns>路径是否安全</returns>
    public static bool IsSafePath(string baseDirectory, string userInputPath)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
            return false;

        if (string.IsNullOrWhiteSpace(userInputPath))
            return false;

        var fullBasePath = Path.GetFullPath(baseDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var fullTargetPath = Path.GetFullPath(Path.IsPathFullyQualified(userInputPath) ?
            userInputPath :
            Path.Combine(fullBasePath, userInputPath));

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (string.Equals(fullTargetPath, fullBasePath, comparison))
            return true;

        return fullTargetPath.StartsWith(
            $"{fullBasePath}{Path.DirectorySeparatorChar}",
            comparison
        );
    }
}