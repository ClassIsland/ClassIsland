namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 创建和解析不依赖应用容器绝对路径的导入文件引用。
/// </summary>
internal static class PortableImportedFileReference
{
    private const string DocumentsDirectoryName = "Documents";
    private const string ClassIslandDirectoryName = "ClassIsland";
    private const string DataDirectoryName = "Data";
    private const string ImportedFilesDirectoryName = "ImportedFiles";

    internal const string Prefix = "_classisland-imported:";

    internal static string Create(string path, string importedFilesRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(importedFilesRoot);

        var root = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(importedFilesRoot));
        var fullPath = Path.GetFullPath(path);
        EnsurePathIsInsideRoot(fullPath, root);

        var relativePath = Path.GetRelativePath(root, fullPath);
        var segments = relativePath
            .Split(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString);
        return Prefix + string.Join('/', segments);
    }

    internal static bool TryResolve(
        string? reference,
        string importedFilesRoot,
        bool migrateLegacyAppleAbsolutePath,
        out string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(importedFilesRoot);

        path = reference ?? string.Empty;
        if (!TryGetRelativeSegments(
                reference,
                migrateLegacyAppleAbsolutePath,
                out var segments))
        {
            return false;
        }

        var root = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(importedFilesRoot));
        var fullPath = Path.GetFullPath(Path.Combine([root, .. segments]));
        EnsurePathIsInsideRoot(fullPath, root);
        path = fullPath;
        return true;
    }

    internal static bool TryGetItemDirectoryName(
        string? reference,
        bool migrateLegacyAppleAbsolutePath,
        out string directoryName)
    {
        directoryName = string.Empty;
        if (!TryGetRelativeSegments(
                reference,
                migrateLegacyAppleAbsolutePath,
                out var segments))
        {
            return false;
        }

        directoryName = segments[0];
        return directoryName is not "." and not ".." &&
               Path.GetFileName(directoryName) == directoryName;
    }

    private static bool TryGetRelativeSegments(
        string? reference,
        bool migrateLegacyAppleAbsolutePath,
        out string[] segments)
    {
        segments = [];
        if (string.IsNullOrWhiteSpace(reference))
        {
            return false;
        }

        if (reference.StartsWith(Prefix, StringComparison.Ordinal))
        {
            var encodedRelativePath = reference[Prefix.Length..];
            if (string.IsNullOrWhiteSpace(encodedRelativePath))
            {
                throw new FormatException("导入文件引用缺少相对路径。");
            }

            segments = encodedRelativePath
                .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.UnescapeDataString)
                .ToArray();
            ValidateRelativeSegments(segments);
            return true;
        }

        if (!migrateLegacyAppleAbsolutePath)
        {
            return false;
        }

        if (!reference.StartsWith("/", StringComparison.Ordinal) ||
            reference.Contains('\\'))
        {
            return false;
        }

        var pathSegments = reference.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries);
        if (!TryGetAppleDocumentsIndex(pathSegments, out var documentsIndex))
        {
            return false;
        }

        var importedFilesIndex = Array.FindLastIndex(
            pathSegments,
            segment => string.Equals(
                segment,
                ImportedFilesDirectoryName,
                StringComparison.Ordinal));
        if (!HasKnownAppleImportedFilesParent(
                pathSegments,
                importedFilesIndex,
                documentsIndex) ||
            importedFilesIndex >= pathSegments.Length - 1)
        {
            return false;
        }

        segments = pathSegments[(importedFilesIndex + 1)..];
        ValidateRelativeSegments(segments);
        return true;
    }

    private static bool TryGetAppleDocumentsIndex(
        IReadOnlyList<string> pathSegments,
        out int documentsIndex)
    {
        documentsIndex = -1;
        var offset = pathSegments.Count > 0 &&
                     string.Equals(
                         pathSegments[0],
                         "private",
                         StringComparison.Ordinal)
            ? 1
            : 0;
        if (pathSegments.Count <= offset + 6 ||
            !string.Equals(
                pathSegments[offset],
                "var",
                StringComparison.Ordinal) ||
            !string.Equals(
                pathSegments[offset + 1],
                "mobile",
                StringComparison.Ordinal) ||
            !string.Equals(
                pathSegments[offset + 2],
                "Containers",
                StringComparison.Ordinal) ||
            !string.Equals(
                pathSegments[offset + 3],
                "Data",
                StringComparison.Ordinal) ||
            !string.Equals(
                pathSegments[offset + 4],
                "Application",
                StringComparison.Ordinal) ||
            !Guid.TryParse(pathSegments[offset + 5], out _) ||
            !string.Equals(
                pathSegments[offset + 6],
                DocumentsDirectoryName,
                StringComparison.Ordinal))
        {
            return false;
        }

        documentsIndex = offset + 6;
        return true;
    }

    private static bool HasKnownAppleImportedFilesParent(
        IReadOnlyList<string> pathSegments,
        int importedFilesIndex,
        int documentsIndex)
    {
        // 当前目录：Documents/ClassIsland/ImportedFiles。
        if (importedFilesIndex == documentsIndex + 2 &&
            string.Equals(
                pathSegments[documentsIndex],
                DocumentsDirectoryName,
                StringComparison.Ordinal) &&
            string.Equals(
                pathSegments[documentsIndex + 1],
                ClassIslandDirectoryName,
                StringComparison.Ordinal))
        {
            return true;
        }

        // 兼容早期目录：Documents/ClassIsland/Data/ImportedFiles。
        return importedFilesIndex == documentsIndex + 3 &&
               string.Equals(
                   pathSegments[documentsIndex],
                   DocumentsDirectoryName,
                   StringComparison.Ordinal) &&
               string.Equals(
                   pathSegments[documentsIndex + 1],
                   ClassIslandDirectoryName,
                   StringComparison.Ordinal) &&
               string.Equals(
                   pathSegments[documentsIndex + 2],
                   DataDirectoryName,
                   StringComparison.Ordinal);
    }

    private static void ValidateRelativeSegments(IReadOnlyList<string> segments)
    {
        if (segments.Count == 0 || segments.Any(segment =>
                string.IsNullOrWhiteSpace(segment) ||
                segment is "." or ".." ||
                segment.Contains('/') ||
                segment.Contains('\\') ||
                segment.IndexOf('\0') >= 0))
        {
            throw new FormatException("导入文件引用包含无效路径。");
        }
    }

    private static void EnsurePathIsInsideRoot(string path, string root)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var rootWithSeparator = root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootWithSeparator, comparison))
        {
            throw new ArgumentException(
                "路径不在应用导入文件目录中。",
                nameof(path));
        }
    }
}
