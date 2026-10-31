using System.Security.Cryptography;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Abstraction.Stubs.Services;

/// <inheritdoc />
public class AvaloniaDefaultPlatformFilePickerService : IPlatformFilePickerService
{
    private const string FileBookmarkSchema = "_elysia-bookmark:";
    private const string FolderBookmarkSchema = "_cyrene-bookmark:";
    
    /// <inheritdoc />
    public async Task<List<string>> OpenFilesPickerAsync(FilePickerOpenOptions options, TopLevel root)
    {
        var list =  (await root.StorageProvider.OpenFilePickerAsync(options))
            .ToList();
        var r = new List<string>();
        foreach (var file in list)
        {
            if (file.TryGetLocalPath() is {} path)
            {
                r.Add(path);
                continue;
            }

            if (file.CanBookmark)
            {
                r.Add(FileBookmarkSchema + await file.SaveBookmarkAsync());
            }
        }

        foreach (var file in list)
        {
            file.Dispose();
        }

        return r;
    }

    /// <inheritdoc />
    public async Task<List<Uri>> OpenBlobFilesPickerAsync(BlobFilePickerOpenOptions options, TopLevel root)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfNegative(options.MaxFileSize);

        var maxFileSize = options.MaxFileSize;
        var files = await root.StorageProvider.OpenFilePickerAsync(options);
        try
        {
            var result = new List<Uri>();
            var blobRoot = Path.GetFullPath(PlatformServices.BlobRootFolderPath);
            foreach (var file in files)
            {
                await using var stream = await file.OpenReadAsync();
                if (await StoreBlobAsync(stream, maxFileSize, blobRoot) is { } uri)
                {
                    result.Add(uri);
                }
            }

            return result;
        }
        finally
        {
            foreach (var file in files)
            {
                file.Dispose();
            }
        }
    }

    private static async Task<Uri?> StoreBlobAsync(Stream stream, long maxFileSize, string blobRoot)
    {
        Directory.CreateDirectory(blobRoot);
        var temporaryPath = Path.Combine(blobRoot, $"{Guid.NewGuid():N}.tmp");
        try
        {
            string hash;
            using (var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                await using var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write,
                    FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
                var buffer = new byte[81920];
                long size = 0;
                int count;
                while ((count = await stream.ReadAsync(buffer.AsMemory())) != 0)
                {
                    if (count > maxFileSize - size)
                    {
                        return null;
                    }

                    size += count;
                    hasher.AppendData(buffer, 0, count);
                    await output.WriteAsync(buffer.AsMemory(0, count));
                }

                hash = Convert.ToHexStringLower(hasher.GetHashAndReset());
            }

            var directory = Path.Combine(blobRoot, hash[..2]);
            Directory.CreateDirectory(directory);
            var destination = Path.Combine(directory, hash);
            try
            {
                File.Move(temporaryPath, destination);
            }
            catch (IOException) when (File.Exists(destination))
            {
                // 相同内容可能已由之前或并发的选取操作存储。
            }

            return new Uri($"avares://*blobs/sha256/{hash}");
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    /// <inheritdoc />
    public async Task<string?> SaveFilePickerAsync(FilePickerSaveOptions options, TopLevel root)
    {
        var file = await root.StorageProvider.SaveFilePickerAsync(options);
        if (file == null)
        {
            return null;
        }
        if (file.TryGetLocalPath() is {} path)
        {
            if (!File.Exists(path))
            {
                await File.Create(path).DisposeAsync();
            }
            file.Dispose();
            return path;
        }
        
        if (!file.CanBookmark)
            return null;
        var path2 = FileBookmarkSchema + await file.SaveBookmarkAsync();
        file.Dispose();
        return path2;

    }

    /// <inheritdoc />
    public async Task<List<string>> OpenFoldersPickerAsync(FolderPickerOpenOptions options, TopLevel root)
    {
        var list = await root.StorageProvider.OpenFolderPickerAsync(options);
        var r = new List<string>();
        foreach (var file in list)
        {
            if (file.TryGetLocalPath() is {} path)
            {
                r.Add(path);
                continue;
            }

            if (file.CanBookmark)
            {
                r.Add(FolderBookmarkSchema + await file.SaveBookmarkAsync());
            }
        }
        
        foreach (var file in list)
        {
            file.Dispose();
        }

        return r;
    }

    /// <inheritdoc />
    public async Task<IStorageFile?> GetFileAsync(string path, TopLevel root)
    {
        if (path.StartsWith(FileBookmarkSchema) && 
            await root.StorageProvider.OpenFileBookmarkAsync(path[FileBookmarkSchema.Length..]) is {} bookmarkFile)
        {
            return bookmarkFile;
        }

        return await root.StorageProvider.TryGetFileFromPathAsync(path);
    }

    /// <inheritdoc />
    public async Task<IStorageFolder?> GetFolderAsync(string path, TopLevel root)
    {
        if (path.StartsWith(FolderBookmarkSchema) && 
            await root.StorageProvider.OpenFolderBookmarkAsync(path[FolderBookmarkSchema.Length..]) is {} bookmarkFolder)
        {
            return bookmarkFolder;
        }

        return await root.StorageProvider.TryGetFolderFromPathAsync(path);
    }

    /// <inheritdoc />
    public bool IsBookmark(string path)
    {
        return path.StartsWith(FileBookmarkSchema) || path.StartsWith(FolderBookmarkSchema);
    }
}
