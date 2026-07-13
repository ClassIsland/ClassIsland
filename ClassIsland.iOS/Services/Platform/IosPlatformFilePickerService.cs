using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ClassIsland.Core;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Platforms.Abstraction.Stubs.Services;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// 在 security-scoped resource 有效期间将选择内容暂存到应用沙盒。
/// </summary>
internal sealed class IosPlatformFilePickerService : AvaloniaDefaultPlatformFilePickerService
{
    private readonly StorageItemMaterializer _materializer;

    public IosPlatformFilePickerService()
    {
        // 部分调用方会长期保存所选图片/音频路径，整包导入也需要跨越一次
        // 用户手动重开，因此不能使用可能被系统清理的 Caches 目录。
        _materializer = new StorageItemMaterializer(
            CommonDirectories.AppImportedFilesFolderPath);
    }

    public override Task<List<string>> MaterializeFilesAsync(
        IReadOnlyList<IStorageFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        return _materializer.MaterializeFilesAsync(files);
    }

    public override async Task<List<string>> OpenFoldersPickerAsync(
        FolderPickerOpenOptions options,
        TopLevel root)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(root);

        var folders = await root.StorageProvider.OpenFolderPickerAsync(options);
        return await _materializer.MaterializeFoldersAsync(folders);
    }

}
