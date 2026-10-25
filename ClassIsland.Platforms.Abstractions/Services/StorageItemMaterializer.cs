using System.Buffers;
using Avalonia.Platform.Storage;

namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 将平台 storage item 复制到应用可持续访问的本地目录。
/// </summary>
internal sealed class StorageItemMaterializer
{
    private const int MaximumFolderDepth = 64;
    private const int CopyBufferSize = 81920;
    internal const int DefaultMaximumFileCount = 4096;
    internal const long DefaultMaximumFileLength = 256L * 1024 * 1024;
    internal const long DefaultMaximumTotalLength = 1024L * 1024 * 1024;

    private readonly string _stagingRoot;
    private readonly int _maximumFileCount;
    private readonly long _maximumFileLength;
    private readonly long _maximumTotalLength;

    public StorageItemMaterializer(
        string stagingRoot,
        int maximumFileCount = DefaultMaximumFileCount,
        long maximumFileLength = DefaultMaximumFileLength,
        long maximumTotalLength = DefaultMaximumTotalLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingRoot);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumFileCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumFileLength);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumTotalLength);

        _stagingRoot = stagingRoot;
        _maximumFileCount = maximumFileCount;
        _maximumFileLength = maximumFileLength;
        _maximumTotalLength = maximumTotalLength;
    }

    public async Task<List<string>> MaterializeFilesAsync(
        IReadOnlyList<IStorageFile> files,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);
        if (files.Count == 0)
        {
            return [];
        }

        string? operationDirectory = null;
        try
        {
            if (files.Count > _maximumFileCount)
            {
                throw new IOException(
                    $"选中的文件数超过上限 {_maximumFileCount}，无法安全导入。");
            }

            operationDirectory = CreateOperationDirectory();
            var result = new List<string>(files.Count);
            var budget = CreateCopyBudget();
            foreach (var file in files)
            {
                result.Add(await CopyFileAsync(
                    file,
                    operationDirectory,
                    budget,
                    cancellationToken));
            }

            return result;
        }
        catch
        {
            if (operationDirectory != null)
            {
                TryDeleteDirectory(operationDirectory);
            }

            throw;
        }
        finally
        {
            DisposeItems(files);
        }
    }

    public async Task<List<string>> MaterializeFoldersAsync(
        IReadOnlyList<IStorageFolder> folders,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(folders);
        if (folders.Count == 0)
        {
            return [];
        }

        string? operationDirectory = null;
        try
        {
            if (folders.Count > _maximumFileCount)
            {
                throw new IOException(
                    $"选中的目录数超过上限 {_maximumFileCount}，无法安全导入。");
            }

            operationDirectory = CreateOperationDirectory();
            var result = new List<string>(folders.Count);
            var budget = CreateCopyBudget();
            foreach (var folder in folders)
            {
                var destination = GetUniquePath(
                    operationDirectory,
                    SafeArchivePath.SanitizeFileNameSegment(
                        folder.Name,
                        "Folder"));
                Directory.CreateDirectory(destination);
                await CopyFolderContentsAsync(
                    folder,
                    destination,
                    budget,
                    0,
                    cancellationToken);
                result.Add(destination);
            }

            return result;
        }
        catch
        {
            if (operationDirectory != null)
            {
                TryDeleteDirectory(operationDirectory);
            }

            throw;
        }
        finally
        {
            DisposeItems(folders);
        }
    }

    /// <summary>
    /// 清理超过指定保留时间的完整导入操作目录。
    /// </summary>
    public int DeleteOperationsOlderThan(TimeSpan retention)
    {
        if (retention < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retention));
        }

        if (!Directory.Exists(_stagingRoot))
        {
            return 0;
        }

        var cutoff = DateTime.UtcNow - retention;
        var deleted = 0;
        foreach (var directory in Directory.EnumerateDirectories(_stagingRoot))
        {
            try
            {
                if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0 ||
                    Directory.GetLastWriteTimeUtc(directory) > cutoff)
                {
                    continue;
                }

                Directory.Delete(directory, true);
                deleted++;
            }
            catch
            {
                // 文件可能仍被正在进行的导入占用，留待下一次清理。
            }
        }

        return deleted;
    }

    private async Task CopyFolderContentsAsync(
        IStorageFolder source,
        string destination,
        CopyBudget budget,
        int depth,
        CancellationToken cancellationToken)
    {
        if (depth >= MaximumFolderDepth)
        {
            throw new IOException($"选中的目录层级超过 {MaximumFolderDepth} 层，无法安全导入。");
        }

        await foreach (var item in source.GetItemsAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (item)
            {
                switch (item)
                {
                    case IStorageFile file:
                        await CopyFileAsync(
                            file,
                            destination,
                            budget,
                            cancellationToken);
                        break;
                    case IStorageFolder folder:
                    {
                        var childDestination = GetUniquePath(
                            destination,
                            SafeArchivePath.SanitizeFileNameSegment(
                                folder.Name,
                                "Folder"));
                        Directory.CreateDirectory(childDestination);
                        await CopyFolderContentsAsync(
                            folder,
                            childDestination,
                            budget,
                            depth + 1,
                            cancellationToken);
                        break;
                    }
                }
            }
        }
    }

    private static async Task<string> CopyFileAsync(
        IStorageFile source,
        string destinationDirectory,
        CopyBudget budget,
        CancellationToken cancellationToken)
    {
        var destination = GetUniquePath(
            destinationDirectory,
            SafeArchivePath.SanitizeFileNameSegment(source.Name, "File"));
        try
        {
            budget.BeginFile();
            await using var input = await source.OpenReadAsync();
            await using var output = new FileStream(
                destination,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                CopyBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
            try
            {
                long fileLength = 0;
                while (true)
                {
                    var bytesRead = await input.ReadAsync(
                        buffer.AsMemory(0, CopyBufferSize),
                        cancellationToken);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    budget.AccountBytes(ref fileLength, bytesRead);
                    await output.WriteAsync(
                        buffer.AsMemory(0, bytesRead),
                        cancellationToken);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            await output.FlushAsync(cancellationToken);
            return destination;
        }
        catch
        {
            TryDeleteFile(destination);
            throw;
        }
    }

    private string CreateOperationDirectory()
    {
        Directory.CreateDirectory(_stagingRoot);
        var path = Path.Combine(_stagingRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private CopyBudget CreateCopyBudget() => new(
        _maximumFileCount,
        _maximumFileLength,
        _maximumTotalLength);

    private static void DisposeItems<T>(IEnumerable<T> items)
        where T : IDisposable
    {
        foreach (var item in items)
        {
            try
            {
                item.Dispose();
            }
            catch
            {
                // 释放 security-scoped resource 失败不应覆盖导入结果或原始异常。
            }
        }
    }

    private static string GetUniquePath(string directory, string name)
    {
        var path = Path.Combine(directory, name);
        if (!PortableNameExists(directory, name))
        {
            return path;
        }

        var stem = Path.GetFileNameWithoutExtension(name);
        var extension = Path.GetExtension(name);
        for (var index = 2; ; index++)
        {
            path = Path.Combine(directory, $"{stem} ({index}){extension}");
            if (!PortableNameExists(directory, Path.GetFileName(path)))
            {
                return path;
            }
        }
    }

    private static bool PortableNameExists(string directory, string name) =>
        Directory.EnumerateFileSystemEntries(directory)
            .Any(path => string.Equals(
                Path.GetFileName(path),
                name,
                StringComparison.OrdinalIgnoreCase));

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, true);
        }
        catch
        {
            // 保留原始导入异常；残留内容可由用户在 iOS 存储设置中清理。
        }
    }

    internal static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // 保留原始导入异常；残留内容可由用户在 iOS 存储设置中清理。
        }
    }

    private sealed class CopyBudget(
        int maximumFileCount,
        long maximumFileLength,
        long maximumTotalLength)
    {
        private int _fileCount;
        private long _totalLength;

        public void BeginFile()
        {
            if (_fileCount >= maximumFileCount)
            {
                throw new IOException(
                    $"选中的文件数超过上限 {maximumFileCount}，无法安全导入。");
            }

            _fileCount++;
        }

        public void AccountBytes(ref long fileLength, int bytesRead)
        {
            if (fileLength > maximumFileLength - bytesRead)
            {
                throw new IOException(
                    $"选中的单个文件大小超过上限 {maximumFileLength} 字节，无法安全导入。");
            }

            if (_totalLength > maximumTotalLength - bytesRead)
            {
                throw new IOException(
                    $"选中文件的总大小超过上限 {maximumTotalLength} 字节，无法安全导入。");
            }

            fileLength += bytesRead;
            _totalLength += bytesRead;
        }
    }
}
