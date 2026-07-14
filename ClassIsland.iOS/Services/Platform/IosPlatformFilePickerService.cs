using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ClassIsland.Core;
using ClassIsland.Core.Helpers;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Platforms.Abstraction.Stubs.Services;

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

    public async Task<List<string>> OpenPersistentFilesPickerAsync(
        FilePickerOpenOptions options,
        TopLevel root)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(root);

        var files = await root.StorageProvider.OpenFilePickerAsync(options);
        return await PersistentImportedFileService.ImportAsync(files);
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
