namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 将不受信任的单段目录名安全解析为指定根目录的直接子目录。
/// </summary>
internal static class SafeChildDirectoryPath
{
    /// <summary>
    /// 验证不受信任的名称可安全用作根目录下的单段子项名称。
    /// </summary>
    public static void ValidateName(string childDirectoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(childDirectoryName);

        if (!string.Equals(childDirectoryName, childDirectoryName.Trim(), StringComparison.Ordinal) ||
            childDirectoryName is "." or ".." ||
            childDirectoryName.IndexOfAny(['/', '\\']) >= 0 ||
            Path.IsPathRooted(childDirectoryName) ||
            childDirectoryName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new InvalidDataException("目录标识必须是不含路径分隔符的安全单段名称。");
        }
    }

    public static string Resolve(string rootDirectory, string childDirectoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        ValidateName(childDirectoryName);

        var rootPath = Path.GetFullPath(rootDirectory);
        var targetPath = Path.GetFullPath(Path.Combine(rootPath, childDirectoryName));
        var relativePath = Path.GetRelativePath(rootPath, targetPath);
        if (Path.IsPathRooted(relativePath) ||
            relativePath == ".." ||
            relativePath.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
        {
            throw new InvalidDataException("目录标识解析后超出允许的根目录。");
        }

        return targetPath;
    }
}
