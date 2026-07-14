using Avalonia.Controls;
using Avalonia.Platform.Storage;

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
    /// 将平台提供的文件转换为调用方可持续读取的本地路径。
    /// </summary>
    /// <param name="files">平台提供的文件。</param>
    /// <returns>可持续读取的本地路径列表。</returns>
    /// <remarks>平台实现可能在转换后释放传入文件，调用方不得继续使用这些对象。</remarks>
    Task<List<string>> MaterializeFilesAsync(IReadOnlyList<IStorageFile> files);

    /// <summary>
    /// 打开文件保存选择器
    /// </summary>
    /// <param name="options">文件浏览器选项</param>
    /// <param name="root">根窗口</param>
    /// <returns>选择的文件路径</returns>
    /// <remarks>
    /// 仅适用于能够提供稳定本地路径的平台。新增导出流程应优先使用
    /// <see cref="SaveFileAsync"/>，以兼容 iOS security-scoped resource。
    /// </remarks>
    Task<string?> SaveFilePickerAsync(FilePickerSaveOptions options, TopLevel root);

    /// <summary>
    /// 选择保存目标，并在平台授予的写入权限有效期内写入内容。
    /// </summary>
    /// <param name="options">文件浏览器选项</param>
    /// <param name="root">根窗口</param>
    /// <param name="writer">向已授权目标流写入内容的回调</param>
    /// <returns>保存目标的本地路径或显示名称；取消时返回 <see langword="null"/>。</returns>
    Task<string?> SaveFileAsync(
        FilePickerSaveOptions options,
        TopLevel root,
        Func<Stream, Task> writer);

    /// <summary>
    /// 打开文件夹打开选择器
    /// </summary>
    /// <param name="options">文件浏览器选项</param>
    /// <param name="root">根窗口</param>
    /// <returns>选择的文件夹完整路径列表</returns>
    Task<List<string>> OpenFoldersPickerAsync(FolderPickerOpenOptions options, TopLevel root);
    
}
