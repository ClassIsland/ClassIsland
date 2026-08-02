namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 规范化不受信任的相对路径，并确保解析结果位于指定根目录内。
/// </summary>
internal static class SafeRelativePath
{
    public static string Normalize(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        if (relativePath.IndexOf('\0') >= 0 ||
            Path.IsPathRooted(relativePath))
        {
            throw new InvalidDataException("路径必须是安全的相对路径。");
        }

        var normalized = relativePath.Replace('\\', '/').TrimEnd('/');
        if (normalized.Length == 0 ||
            normalized.StartsWith("/", StringComparison.Ordinal) ||
            (normalized.Length >= 3 &&
             char.IsAsciiLetter(normalized[0]) &&
             normalized[1] == ':' &&
             normalized[2] == '/'))
        {
            throw new InvalidDataException("路径必须是安全的相对路径。");
        }

        var segments = normalized.Split('/');
        if (segments.Any(x => x.Length == 0 || x is "." or ".."))
        {
            throw new InvalidDataException("路径不能包含空段、. 或 .. 段。");
        }

        return string.Join('/', segments);
    }

    public static string ResolveUnderRoot(string rootDirectory, string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        var normalized = Normalize(relativePath);
        var rootPath = Path.GetFullPath(rootDirectory);
        var targetPath = Path.GetFullPath(Path.Combine(
            rootPath,
            normalized.Replace('/', Path.DirectorySeparatorChar)));
        var pathFromRoot = Path.GetRelativePath(rootPath, targetPath);
        if (Path.IsPathRooted(pathFromRoot) ||
            pathFromRoot == ".." ||
            pathFromRoot.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
        {
            throw new InvalidDataException("路径解析后超出允许的根目录。");
        }

        return targetPath;
    }
}
