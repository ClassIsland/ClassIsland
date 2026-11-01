using Avalonia.Platform.Storage;

namespace ClassIsland.Platforms.Abstraction.Models;

/// <summary>
/// Blob 文件选取选项。
/// </summary>
public class BlobFilePickerOpenOptions : FilePickerOpenOptions
{
    /// <summary>
    /// 单个文件允许的最大字节数，默认为 50 MiB。超限文件将被跳过，零仅允许空文件。
    /// </summary>
    public long MaxFileSize { get; set; } = 50L * 1024 * 1024;
}
