namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 将归档路径规范化为可在受支持平台间安全往返的相对路径。
/// </summary>
internal static class SafeArchivePath
{
    internal static string SanitizeFileNameSegment(
        string? name,
        string fallback)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fallback);

        var candidate = Path.GetFileName(name?.Trim());
        if (string.IsNullOrWhiteSpace(candidate) || candidate is "." or "..")
        {
            return fallback;
        }

        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            candidate = candidate.Replace(invalidCharacter, '_');
        }

        foreach (var portableInvalidCharacter in "<>:\"/\\|?*")
        {
            candidate = candidate.Replace(portableInvalidCharacter, '_');
        }

        candidate = new string(candidate
            .Select(character => character < ' ' ? '_' : character)
            .ToArray())
            .TrimEnd(' ', '.');
        if (IsWindowsReservedName(candidate))
        {
            candidate = "_" + candidate;
        }

        return string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;
    }

    internal static string NormalizeRelativePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (path.IndexOf('\0') >= 0)
        {
            throw new InvalidDataException("归档路径包含空字符。");
        }

        var normalized = path.Replace('\\', '/');
        if (normalized.StartsWith("/", StringComparison.Ordinal) ||
            IsWindowsAbsolutePath(normalized))
        {
            throw new InvalidDataException($"归档包含绝对路径：{path}");
        }

        var hasTrailingSeparator = normalized.EndsWith(
            "/",
            StringComparison.Ordinal);
        var segments = normalized.Split('/', StringSplitOptions.None);
        var segmentCount = hasTrailingSeparator
            ? segments.Length - 1
            : segments.Length;
        if (segmentCount == 0 ||
            segments.Take(segmentCount).Any(IsUnsafeSegment))
        {
            throw new InvalidDataException($"归档包含无效相对路径：{path}");
        }

        return string.Join('/', segments.Take(segmentCount)) +
               (hasTrailingSeparator ? "/" : string.Empty);
    }

    internal static string NormalizeFileSystemRelativePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (Path.DirectorySeparatorChar != '\\' && path.Contains('\\'))
        {
            throw new InvalidDataException(
                $"文件名包含无法跨平台安全归档的反斜杠：{path}");
        }

        return NormalizeRelativePath(
            path.Replace(Path.DirectorySeparatorChar, '/'));
    }

    private static bool IsUnsafeSegment(string segment) =>
        string.IsNullOrEmpty(segment) ||
        segment is "." or ".." ||
        segment.Any(character =>
            character < ' ' || "<>:\"|?*".Contains(character)) ||
        segment.EndsWith(' ') ||
        segment.EndsWith('.') ||
        IsWindowsReservedName(segment);

    private static bool IsWindowsReservedName(string segment)
    {
        var stem = segment.Split('.', 2)[0];
        return stem.Equals("CON", StringComparison.OrdinalIgnoreCase) ||
               stem.Equals("PRN", StringComparison.OrdinalIgnoreCase) ||
               stem.Equals("AUX", StringComparison.OrdinalIgnoreCase) ||
               stem.Equals("NUL", StringComparison.OrdinalIgnoreCase) ||
               stem.Length == 4 &&
               (stem.StartsWith("COM", StringComparison.OrdinalIgnoreCase) ||
                stem.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)) &&
               stem[3] is >= '1' and <= '9';
    }

    private static bool IsWindowsAbsolutePath(string path) =>
        path.Length >= 3 &&
        char.IsAsciiLetter(path[0]) &&
        path[1] == ':' &&
        path[2] == '/';
}
