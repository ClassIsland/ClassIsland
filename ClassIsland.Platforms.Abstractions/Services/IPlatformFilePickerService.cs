using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ClassIsland.Platforms.Abstraction.Models;

namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 平台自定义文件选取服务
/// </summary>
public interface IPlatformFilePickerService
{
    /// <summary>
    /// 打开文件打开选择器
    /// </summary>
    /// <param name="options">文件浏览器选项</param>
    /// <param name="root">根窗口</param>
    /// <returns>选择的文件完整路径列表</returns>
    Task<List<string>> OpenFilesPickerAsync(FilePickerOpenOptions options, TopLevel root);

    /// <summary>
    /// 选取文件并按 SHA-256 存储为 Blob，跳过超出大小限制的文件。
    /// </summary>
    /// <param name="options">文件浏览器及大小限制选项</param>
    /// <param name="root">根窗口</param>
    /// <returns>按选取顺序排列的 Blob URI 列表；取消或全部超限时为空</returns>
    /// <exception cref="ArgumentOutOfRangeException">最大文件大小为负数。</exception>
    Task<List<Uri>> OpenBlobFilesPickerAsync(BlobFilePickerOpenOptions options, TopLevel root);

    /// <summary>
    /// 打开文件保存选择器
    /// </summary>
    /// <param name="options">文件浏览器选项</param>
    /// <param name="root">根窗口</param>
    /// <returns>选择的文件路径</returns>
    Task<string?> SaveFilePickerAsync(FilePickerSaveOptions options, TopLevel root);

    /// <summary>
    /// 打开文件夹打开选择器
    /// </summary>
    /// <param name="options">文件浏览器选项</param>
    /// <param name="root">根窗口</param>
    /// <returns>选择的文件夹完整路径列表</returns>
    Task<List<string>> OpenFoldersPickerAsync(FolderPickerOpenOptions options, TopLevel root);

    /// <summary>
    /// 获取一个文件。
    /// </summary>
    /// <param name="path">文件路径或书签</param>
    /// <param name="root">根窗口</param>
    /// <returns>打开的文件流</returns>
    Task<IStorageFile?> GetFileAsync(string path, TopLevel root);

    /// <summary>
    /// 获取一个文件夹。
    /// </summary>
    /// <param name="path">文件路径或书签</param>
    /// <param name="root">根窗口</param>
    /// <returns>打开的文件流</returns>
    Task<IStorageFolder?> GetFolderAsync(string path, TopLevel root);

    /// <summary>
    /// 判断该路径是否是间接的书签
    /// </summary>
    /// <returns>是否是书签</returns>
    bool IsBookmark(string path);

}
