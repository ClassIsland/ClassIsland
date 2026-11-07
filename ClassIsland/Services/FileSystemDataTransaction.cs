using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Services;

internal sealed class FileSystemRollbackException : IOException
{
    internal FileSystemRollbackException(
        string rollbackPath,
        Exception operationException,
        Exception rollbackException)
        : base(
            $"数据操作失败，且无法完整回滚。原始数据快照已保留在：{rollbackPath}",
            new AggregateException(operationException, rollbackException))
    {
        RollbackPath = rollbackPath;
    }

    internal string RollbackPath { get; }
}

/// <summary>
/// 对应用数据目录中的一组相对路径执行可回滚文件系统操作。
/// </summary>
internal static class FileSystemDataTransaction
{
    private const string ManifestFileName = "rollback-manifest.json";

    internal static void Execute(
        string liveRoot,
        string rollbackRoot,
        IReadOnlyCollection<string> relativePaths,
        Action operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(liveRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(rollbackRoot);
        ArgumentNullException.ThrowIfNull(relativePaths);
        ArgumentNullException.ThrowIfNull(operation);

        var live = Path.TrimEndingDirectorySeparator(Path.GetFullPath(liveRoot));
        var rollback = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rollbackRoot));
        var paths = relativePaths
            .Select(ValidateRelativePath)
            .Distinct(GetPathComparer())
            .ToList();
        var preserveRollback = false;

        try
        {
            EnsureDestinationIsOutsideSource(live, rollback);
            EnsureExistingPathComponentsAreNotLinks(live, live);
            EnsureExistingPathComponentsAreNotLinks(rollback, rollback);
            Directory.CreateDirectory(rollback);
            EnsureDirectoryIsNotLink(rollback);
            if (Directory.EnumerateFileSystemEntries(rollback).Any())
            {
                throw new IOException("回滚快照目录必须为空。");
            }

            var manifest = CaptureSnapshot(live, rollback, paths);
            File.WriteAllText(
                Path.Combine(rollback, ManifestFileName),
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));

            try
            {
                operation();
            }
            catch (Exception operationException)
            {
                try
                {
                    RestoreSnapshot(live, rollback, manifest);
                }
                catch (Exception rollbackException)
                {
                    preserveRollback = true;
                    throw new FileSystemRollbackException(
                        rollback,
                        operationException,
                        rollbackException);
                }

                throw;
            }
        }
        finally
        {
            if (!preserveRollback)
            {
                TryDeleteDirectory(rollback);
            }
        }
    }

    internal static void CopyDirectoryStrict(
        string source,
        string destination,
        bool overwrite = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);

        var sourceRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(source));
        var destinationRoot = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(destination));
        EnsureDirectoryIsNotLink(sourceRoot);
        EnsureExistingPathComponentsAreNotLinks(
            destinationRoot,
            Path.GetDirectoryName(destinationRoot) ?? destinationRoot);
        EnsureDestinationIsOutsideSource(sourceRoot, destinationRoot);
        CopyDirectoryCore(sourceRoot, destinationRoot, overwrite);
    }

    internal static void CopyFileStrict(
        string source,
        string destination,
        bool overwrite = false)
    {
        EnsureFileIsNotLink(source);
        var fullDestination = Path.GetFullPath(destination);
        var destinationDirectory = Path.GetDirectoryName(fullDestination)
                                   ?? throw new InvalidOperationException(
                                       "无法确定文件目标目录。");
        EnsureExistingPathComponentsAreNotLinks(
            fullDestination,
            destinationDirectory);
        Directory.CreateDirectory(destinationDirectory);
        EnsureDirectoryIsNotLink(destinationDirectory);
        File.Copy(source, fullDestination, overwrite);
    }

    internal static void EnsureFileIsNotLink(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("找不到文件。", path);
        }

        RejectReparsePoint(path);
    }

    internal static IEnumerable<string> EnumerateFilesStrict(string root)
    {
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        EnsureDirectoryIsNotLink(fullRoot);
        return EnumerateFilesCore(fullRoot).ToList();
    }

    internal static void DeleteEntry(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return;
        }

        var attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            if ((attributes & FileAttributes.Directory) != 0)
            {
                Directory.Delete(path, false);
            }
            else
            {
                File.Delete(path);
            }

            return;
        }

        if ((attributes & FileAttributes.Directory) != 0)
        {
            Directory.Delete(path, true);
        }
        else
        {
            File.Delete(path);
        }
    }

    internal static void TryDeleteDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            var attributes = File.GetAttributes(path);
            Directory.Delete(
                path,
                (attributes & FileAttributes.ReparsePoint) == 0);
        }
        catch
        {
            // 临时目录清理失败不能掩盖操作结果。
        }
    }

    internal static void TryDeleteFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // 临时文件清理失败不能掩盖操作结果。
        }
    }

    internal static void EnsureDirectoryIsNotLink(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"找不到目录：{path}");
        }

        RejectReparsePoint(path);
    }

    private static List<SnapshotEntry> CaptureSnapshot(
        string liveRoot,
        string rollbackRoot,
        IEnumerable<string> relativePaths)
    {
        var manifest = new List<SnapshotEntry>();
        foreach (var relativePath in relativePaths)
        {
            var source = ResolveInsideRoot(liveRoot, relativePath);
            var destination = ResolveInsideRoot(rollbackRoot, relativePath);
            if (File.Exists(source))
            {
                CopyFileStrict(source, destination, true);
                manifest.Add(new SnapshotEntry(relativePath, SnapshotEntryKind.File));
            }
            else if (Directory.Exists(source))
            {
                CopyDirectoryStrict(source, destination, true);
                manifest.Add(new SnapshotEntry(
                    relativePath,
                    SnapshotEntryKind.Directory));
            }
            else
            {
                manifest.Add(new SnapshotEntry(
                    relativePath,
                    SnapshotEntryKind.Missing));
            }
        }

        return manifest;
    }

    private static void RestoreSnapshot(
        string liveRoot,
        string rollbackRoot,
        IEnumerable<SnapshotEntry> manifest)
    {
        foreach (var entry in manifest)
        {
            var destination = ResolveInsideRoot(liveRoot, entry.RelativePath);
            DeleteEntry(destination);

            var source = ResolveInsideRoot(rollbackRoot, entry.RelativePath);
            switch (entry.Kind)
            {
                case SnapshotEntryKind.File:
                    CopyFileStrict(source, destination, true);
                    break;
                case SnapshotEntryKind.Directory:
                    CopyDirectoryStrict(source, destination, true);
                    break;
                case SnapshotEntryKind.Missing:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private static void CopyDirectoryCore(
        string source,
        string destination,
        bool overwrite)
    {
        EnsureExistingPathComponentsAreNotLinks(
            destination,
            Path.GetDirectoryName(destination) ?? destination);
        Directory.CreateDirectory(destination);
        EnsureDirectoryIsNotLink(destination);
        foreach (var file in Directory.EnumerateFiles(source))
        {
            RejectReparsePoint(file);
            var destinationFile = Path.Combine(
                destination,
                Path.GetFileName(file));
            EnsureExistingPathComponentsAreNotLinks(
                destinationFile,
                destination);
            File.Copy(file, destinationFile, overwrite);
        }

        foreach (var directory in Directory.EnumerateDirectories(source))
        {
            EnsureDirectoryIsNotLink(directory);
            CopyDirectoryCore(
                directory,
                Path.Combine(destination, Path.GetFileName(directory)),
                overwrite);
        }
    }

    private static IEnumerable<string> EnumerateFilesCore(string root)
    {
        foreach (var file in Directory.EnumerateFiles(root))
        {
            RejectReparsePoint(file);
            yield return file;
        }

        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            EnsureDirectoryIsNotLink(directory);
            foreach (var file in EnumerateFilesCore(directory))
            {
                yield return file;
            }
        }
    }

    private static string ValidateRelativePath(string relativePath)
    {
        var normalized = SafeArchivePath.NormalizeRelativePath(relativePath)
            .TrimEnd('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("事务路径不能为空。", nameof(relativePath));
        }

        return normalized.Replace('/', Path.DirectorySeparatorChar);
    }

    private static string ResolveInsideRoot(string root, string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(root, relativePath));
        var pathFromRoot = Path.GetRelativePath(root, path);
        if (Path.IsPathRooted(pathFromRoot) ||
            pathFromRoot == ".." ||
            pathFromRoot.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
        {
            throw new InvalidDataException($"事务路径越界：{relativePath}");
        }

        EnsureExistingPathComponentsAreNotLinks(path, root);
        return path;
    }

    private static void EnsureDestinationIsOutsideSource(
        string source,
        string destination)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (string.Equals(source, destination, comparison) ||
            destination.StartsWith(
                source + Path.DirectorySeparatorChar,
                comparison))
        {
            throw new IOException("目录复制目标不能位于源目录内部。");
        }
    }

    private static void EnsureExistingPathComponentsAreNotLinks(
        string path,
        string trustedRoot)
    {
        // 只检查应用控制的 trustedRoot 及其后代。Darwin 的 /var 等系统
        // 祖先本身可能是合法 symlink，不能把它们当作应用数据越界。
        foreach (var current in TrustedFileSystemPathPolicy
                     .GetControlledComponents(path, trustedRoot))
        {
            try
            {
                RejectReparsePoint(current);
            }
            catch (Exception exception) when (
                exception is FileNotFoundException or DirectoryNotFoundException)
            {
                // 尚未创建的路径段没有可检查的重解析点，继续检查其父级。
            }
        }
    }

    private static void RejectReparsePoint(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new IOException($"不允许复制符号链接或重解析点：{path}");
        }
    }

    private static StringComparer GetPathComparer() =>
        OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    private sealed record SnapshotEntry(
        string RelativePath,
        SnapshotEntryKind Kind);

    private enum SnapshotEntryKind
    {
        Missing,
        File,
        Directory
    }
}
