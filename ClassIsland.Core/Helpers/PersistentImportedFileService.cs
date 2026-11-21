using Avalonia.Platform.Storage;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Core.Helpers;

/// <summary>
/// 将需要长期使用的平台文件或文件夹复制到应用共享目录，并生成可迁移引用。
/// </summary>
internal static class PersistentImportedFileService
{
    public static Task<List<string>> ImportAsync(
        IReadOnlyList<IStorageFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        return ImportItemsAsync(
            files,
            static (materializer, items) =>
                materializer.MaterializeFilesAsync(items));
    }

    public static Task<List<string>> ImportFoldersAsync(
        IReadOnlyList<IStorageFolder> folders)
    {
        ArgumentNullException.ThrowIfNull(folders);
        return ImportItemsAsync(
            folders,
            static (materializer, items) =>
                materializer.MaterializeFoldersAsync(items));
    }

    private static async Task<List<string>> ImportItemsAsync<T>(
        IReadOnlyList<T> items,
        Func<StorageItemMaterializer, IReadOnlyList<T>, Task<List<string>>>
            materializeItemsAsync)
        where T : IStorageItem
    {
        if (items.Count == 0)
        {
            return [];
        }

        var materializer = new StorageItemMaterializer(
            CommonDirectories.AppImportedFilesFolderPath);
        var paths = await materializeItemsAsync(materializer, items);
        if (paths.Count != items.Count)
        {
            DeleteOperationDirectories(paths);
            throw new InvalidOperationException(
                "持久导入结果与选择项数量不一致。");
        }

        try
        {
            return paths.Select(ImportedFileReference.Create).ToList();
        }
        catch
        {
            DeleteOperationDirectories(paths);
            throw;
        }
    }

    private static void DeleteOperationDirectories(IEnumerable<string> paths)
    {
        foreach (var directory in paths
                     .Select(Path.GetDirectoryName)
                     .OfType<string>()
                     .Distinct(StringComparer.Ordinal))
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch
            {
                // 保留原始导入异常。
            }
        }
    }
}
