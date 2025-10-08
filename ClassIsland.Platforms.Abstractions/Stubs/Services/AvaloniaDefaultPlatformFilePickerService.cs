using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Abstraction.Stubs.Services;

/// <inheritdoc />
public class AvaloniaDefaultPlatformFilePickerService : IPlatformFilePickerService
{
    /// <inheritdoc />
    public async Task<List<string>> OpenFilesPickerAsync(FilePickerOpenOptions options, TopLevel root)
    {
        return (await root.StorageProvider.OpenFilePickerAsync(options))
            .Select(x => x.TryGetLocalPath())
            .OfType<string>()
            .ToList();
    }

    /// <inheritdoc />
    public async Task<string?> SaveFilePickerAsync(FilePickerSaveOptions options, TopLevel root)
    {
        return (await root.StorageProvider.SaveFilePickerAsync(options))?.TryGetLocalPath();
    }

    /// <inheritdoc />
    public async Task<List<string>> OpenFoldersPickerAsync(FolderPickerOpenOptions options, TopLevel root)
    {
        return (await root.StorageProvider.OpenFolderPickerAsync(options))
            .Select(x => x.TryGetLocalPath())
            .OfType<string>()
            .ToList();
    }
}