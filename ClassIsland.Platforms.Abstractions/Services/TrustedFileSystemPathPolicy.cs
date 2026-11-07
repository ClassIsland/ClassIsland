namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 计算受信任根目录中由应用控制、需要检查链接的路径组件。
/// </summary>
internal static class TrustedFileSystemPathPolicy
{
    internal static IReadOnlyList<string> GetControlledComponents(
        string path,
        string trustedRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(trustedRoot);

        var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        var fullRoot = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(trustedRoot));
        var relativePath = Path.GetRelativePath(fullRoot, fullPath);
        if (Path.IsPathRooted(relativePath) ||
            relativePath == ".." ||
            relativePath.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal) ||
            Path.AltDirectorySeparatorChar != Path.DirectorySeparatorChar &&
            relativePath.StartsWith(
                $"..{Path.AltDirectorySeparatorChar}",
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"路径不在受信任的文件系统边界内：{path}");
        }

        var components = new List<string> { fullRoot };
        if (relativePath == ".")
        {
            return components;
        }

        var current = fullRoot;
        foreach (var segment in relativePath.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            components.Add(current);
        }

        return components;
    }
}
