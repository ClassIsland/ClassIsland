using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Abstraction.Stubs.Services;

/// <inheritdoc />
public class AvaloniaDefaultPlatformFilePickerService : IPlatformFilePickerService
{
    private const string FileBookmarkSchema = "_elysia-bookmark:";
    private const string FolderBookmarkSchema = "_cyrene-bookmark:";

    /// <inheritdoc />
    public virtual async Task<List<string>> OpenFilesPickerAsync(FilePickerOpenOptions options, TopLevel root)
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
    public virtual async Task<List<string>> MaterializeFilesAsync(IReadOnlyList<IStorageFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        var r = new List<string>();
        foreach (var file in files)
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

        foreach (var file in files)
        {
            file.Dispose();
        }

        return r;
    }

    /// <inheritdoc />
    public virtual async Task<string?> SaveFilePickerAsync(FilePickerSaveOptions options, TopLevel root)
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
    public virtual async Task<string?> SaveFileAsync(
        FilePickerSaveOptions options,
        TopLevel root,
        Func<Stream, Task> writer)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(writer);

        using var file = await root.StorageProvider.SaveFilePickerAsync(options);
        if (file == null)
        {
            return null;
        }

        await using (var output = await file.OpenWriteAsync())
        {
            if (output.CanSeek)
            {
                output.SetLength(0);
            }

            await writer(output);
            await output.FlushAsync();
        }

        return file.TryGetLocalPath() ?? file.Name;
    }

    /// <inheritdoc />
    public virtual async Task<List<string>> OpenFoldersPickerAsync(FolderPickerOpenOptions options, TopLevel root)
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
