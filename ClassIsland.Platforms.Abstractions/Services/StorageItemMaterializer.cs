using Avalonia.Platform.Storage;

namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 将平台 storage item 复制到应用可持续访问的本地目录。
/// </summary>
internal sealed class StorageItemMaterializer(string stagingRoot)
{
    private const int MaximumFolderDepth = 64;

    public async Task<List<string>> MaterializeFilesAsync(
        IReadOnlyList<IStorageFile> files,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);
        if (files.Count == 0)
        {
            return [];
        }

        var operationDirectory = CreateOperationDirectory();
        var result = new List<string>(files.Count);
        try
        {
            foreach (var file in files)
            {
                using (file)
                {
                    result.Add(await CopyFileAsync(file, operationDirectory, cancellationToken));
                }
            }

            return result;
        }
        catch
        {
            TryDeleteDirectory(operationDirectory);
            throw;
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

        var operationDirectory = CreateOperationDirectory();
        var result = new List<string>(folders.Count);
        try
        {
            foreach (var folder in folders)
            {
                using (folder)
                {
                    var destination = GetUniquePath(
                        operationDirectory,
                        GetSafeName(folder.Name, "Folder"));
                    Directory.CreateDirectory(destination);
                    await CopyFolderContentsAsync(folder, destination, 0, cancellationToken);
                    result.Add(destination);
                }
            }

            return result;
        }
        catch
        {
            TryDeleteDirectory(operationDirectory);
            throw;
        }
    }

    private async Task CopyFolderContentsAsync(
        IStorageFolder source,
        string destination,
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
                        await CopyFileAsync(file, destination, cancellationToken);
                        break;
                    case IStorageFolder folder:
                    {
                        var childDestination = GetUniquePath(
                            destination,
                            GetSafeName(folder.Name, "Folder"));
                        Directory.CreateDirectory(childDestination);
                        await CopyFolderContentsAsync(
                            folder,
                            childDestination,
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
        CancellationToken cancellationToken)
    {
        var destination = GetUniquePath(
            destinationDirectory,
            GetSafeName(source.Name, "File"));
        try
        {
            await using var input = await source.OpenReadAsync();
            await using var output = new FileStream(
                destination,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            await input.CopyToAsync(output, cancellationToken);
            return destination;
        }
        catch
        {
            File.Delete(destination);
            throw;
        }
    }

    private string CreateOperationDirectory()
    {
        Directory.CreateDirectory(stagingRoot);
        var path = Path.Combine(stagingRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string GetSafeName(string? name, string fallback)
    {
        var candidate = Path.GetFileName(name?.Trim());
        if (string.IsNullOrWhiteSpace(candidate) || candidate is "." or "..")
        {
            return fallback;
        }

        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            candidate = candidate.Replace(invalidCharacter, '_');
        }

        return string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;
    }

    private static string GetUniquePath(string directory, string name)
    {
        var path = Path.Combine(directory, name);
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return path;
        }

        var stem = Path.GetFileNameWithoutExtension(name);
        var extension = Path.GetExtension(name);
        for (var index = 2; ; index++)
        {
            path = Path.Combine(directory, $"{stem} ({index}){extension}");
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                return path;
            }
        }
    }

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
}
