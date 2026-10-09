using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Services;

/// <summary>
/// 仅将允许的数据条目解压到指定暂存目录。
/// 调用前仍须使用 ZipArchiveSafety 验证条目数量与解压大小。
/// </summary>
internal static class SafeArchiveExtractor
{
    internal static int ExtractSelected(
        ZipArchive archive,
        string destinationRoot,
        IReadOnlySet<string> allowedFiles,
        IReadOnlySet<string> allowedDirectories)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationRoot);
        ArgumentNullException.ThrowIfNull(allowedFiles);
        ArgumentNullException.ThrowIfNull(allowedDirectories);

        var root = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(destinationRoot));
        var entries = archive.Entries
            .Select(entry => InspectEntry(
                entry,
                root,
                allowedFiles,
                allowedDirectories))
            .ToList();

        Directory.CreateDirectory(root);
        var extracted = 0;
        foreach (var inspected in entries.Where(entry => entry.ShouldExtract))
        {
            if (inspected.IsDirectory)
            {
                Directory.CreateDirectory(inspected.TargetPath);
            }
            else
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(inspected.TargetPath) ?? root);
                inspected.Entry.ExtractToFile(inspected.TargetPath, true);
            }

            extracted++;
        }

        return extracted;
    }

    private static InspectedArchiveEntry InspectEntry(
        ZipArchiveEntry entry,
        string destinationRoot,
        IReadOnlySet<string> allowedFiles,
        IReadOnlySet<string> allowedDirectories)
    {
        var normalized = SafeArchivePath.NormalizeRelativePath(entry.FullName);
        var isDirectory = normalized.EndsWith("/", StringComparison.Ordinal);
        var segments = normalized.TrimEnd('/').Split('/');
        var firstSegment = segments[0];

        if (IsSymbolicLink(entry))
        {
            throw new InvalidDataException(
                $"归档包含不支持的符号链接：{entry.FullName}");
        }

        var isAllowedFile = segments.Length == 1 &&
                            !isDirectory &&
                            allowedFiles.Contains(firstSegment);
        var isAllowedDirectory = allowedDirectories.Contains(firstSegment);
        if (isAllowedDirectory && segments.Length == 1 && !isDirectory)
        {
            throw new InvalidDataException(
                $"归档目录 {firstSegment} 被存储为普通文件。");
        }

        if (allowedFiles.Contains(firstSegment) &&
            (segments.Length != 1 || isDirectory))
        {
            throw new InvalidDataException(
                $"归档文件 {firstSegment} 的路径或类型无效。");
        }

        var targetPath = Path.GetFullPath(Path.Combine(
            [destinationRoot, .. segments]));
        EnsureInsideRoot(targetPath, destinationRoot, entry.FullName);

        return new InspectedArchiveEntry(
            entry,
            targetPath,
            isDirectory,
            isAllowedFile || isAllowedDirectory);
    }

    private static bool IsSymbolicLink(ZipArchiveEntry entry)
    {
        const int unixFileTypeMask = 0xF000;
        const int unixSymbolicLink = 0xA000;
        var unixMode = (entry.ExternalAttributes >> 16) & unixFileTypeMask;
        return unixMode == unixSymbolicLink;
    }

    private static void EnsureInsideRoot(
        string path,
        string root,
        string archivePath)
    {
        var relativePath = Path.GetRelativePath(root, path);
        if (Path.IsPathRooted(relativePath) ||
            relativePath == ".." ||
            relativePath.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"归档包含越界路径：{archivePath}");
        }
    }

    private sealed record InspectedArchiveEntry(
        ZipArchiveEntry Entry,
        string TargetPath,
        bool IsDirectory,
        bool ShouldExtract);
}
