using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Abstraction.Stubs.Services;

/// <inheritdoc />
public class AvaloniaDefaultPlatformFilePickerService : IPlatformFilePickerService
{
    /// <inheritdoc />
    public virtual async Task<List<string>> OpenFilesPickerAsync(FilePickerOpenOptions options, TopLevel root)
    {
        return await MaterializeFilesAsync(await root.StorageProvider.OpenFilePickerAsync(options));
    }

    /// <inheritdoc />
    public virtual Task<List<string>> MaterializeFilesAsync(IReadOnlyList<IStorageFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        var paths = files
            .Select(x => x.TryGetLocalPath())
            .OfType<string>()
            .ToList();
        return Task.FromResult(paths);
    }

    /// <inheritdoc />
    public virtual async Task<string?> SaveFilePickerAsync(FilePickerSaveOptions options, TopLevel root)
    {
        return (await root.StorageProvider.SaveFilePickerAsync(options))?.TryGetLocalPath();
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
        return (await root.StorageProvider.OpenFolderPickerAsync(options))
            .Select(x => x.TryGetLocalPath())
            .OfType<string>()
            .ToList();
    }
}
