using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ClassIsland.Core;
using ClassIsland.Core.Helpers;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Platforms.Abstraction.Stubs.Services;
using ClassIsland.Shared;
using Microsoft.Extensions.Logging;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// 在 security-scoped resource 有效期间将选择内容暂存到应用沙盒。
/// </summary>
internal sealed class IosPlatformFilePickerService : AvaloniaDefaultPlatformFilePickerService,
    IPersistentFilePickerService
{
    private const string TemporaryPickerFolderName = "iOSFilePicker";
    private static readonly TimeSpan TemporaryItemRetention = TimeSpan.FromDays(7);
    private int _temporaryItemsCleaned;

    private StorageItemMaterializer CreateTemporaryMaterializer()
    {
        var materializer = new StorageItemMaterializer(Path.Combine(
            CommonDirectories.AppTempFolderPath,
            TemporaryPickerFolderName));
        if (Interlocked.Exchange(ref _temporaryItemsCleaned, 1) == 0)
        {
            materializer.DeleteOperationsOlderThan(TemporaryItemRetention);
        }

        return materializer;
    }

    public override Task<List<string>> MaterializeFilesAsync(
        IReadOnlyList<IStorageFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        return CreateTemporaryMaterializer().MaterializeFilesAsync(files);
    }

    public override Task<List<string>> OpenFilesPickerAsync(
        FilePickerOpenOptions options,
        TopLevel root)
    {
        return OpenAndMaterializeFilesAsync(options, root, MaterializeFilesAsync);
    }

    public Task<List<string>> OpenPersistentFilesPickerAsync(
        FilePickerOpenOptions options,
        TopLevel root)
    {
        return OpenAndMaterializeFilesAsync(options, root, PersistentImportedFileService.ImportAsync);
    }

    private static async Task<List<string>> OpenAndMaterializeFilesAsync(
        FilePickerOpenOptions options,
        TopLevel root,
        Func<IReadOnlyList<IStorageFile>, Task<List<string>>> materializeAsync)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(root);

        var logger = IAppHost.TryGetService<ILogger<IosPlatformFilePickerService>>();
        var stage = "Opening picker";
        try
        {
            logger?.LogInformation("iOS file import: opening picker");
            var files = await IosDocumentImportPicker.OpenAsync(options, root, logger);
            logger?.LogInformation("iOS file import: picker returned {FileCount} files", files.Count);

            stage = "Materializing selected files";
            var paths = await materializeAsync(files);
            logger?.LogInformation("iOS file import: materialized {FileCount} files", paths.Count);
            return paths;
        }
        catch (Exception exception)
        {
            logger?.LogError(exception, "iOS file import failed at {Stage}", stage);
            throw;
        }
    }

    public async Task<List<string>> OpenPersistentFoldersPickerAsync(
        FolderPickerOpenOptions options,
        TopLevel root)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(root);

        var folders = await root.StorageProvider.OpenFolderPickerAsync(options);
        return await PersistentImportedFileService.ImportFoldersAsync(folders);
    }

    public override Task<string?> SaveFilePickerAsync(
        FilePickerSaveOptions options,
        TopLevel root)
    {
        throw new PlatformNotSupportedException(
            "iOS 无法把 security-scoped 保存目标安全地转换为可长期使用的文件路径；请使用 SaveFileAsync 在授权期间写入目标流。");
    }

    public override async Task<List<string>> OpenFoldersPickerAsync(
        FolderPickerOpenOptions options,
        TopLevel root)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(root);

        var folders = await root.StorageProvider.OpenFolderPickerAsync(options);
        return await CreateTemporaryMaterializer().MaterializeFoldersAsync(folders);
    }
}
