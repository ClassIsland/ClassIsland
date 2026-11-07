using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Core.Helpers;

/// <summary>
/// 为复制到应用共享目录的持久导入文件创建不包含沙盒容器 UUID 的引用。
/// </summary>
internal static class ImportedFileReference
{
    /// <summary>
    /// 持久导入文件引用前缀。
    /// </summary>
    public const string Prefix = PortableImportedFileReference.Prefix;

    /// <summary>
    /// 将导入目录中的绝对路径转换为可随应用容器迁移的引用。
    /// </summary>
    public static string Create(string path)
    {
        return PortableImportedFileReference.Create(
            path,
            CommonDirectories.AppImportedFilesFolderPath);
    }

    /// <summary>
    /// 尝试将持久导入文件引用解析到当前应用容器。
    /// </summary>
    public static bool TryResolve(string? reference, out string path)
    {
        return PortableImportedFileReference.TryResolve(
            reference,
            CommonDirectories.AppImportedFilesFolderPath,
            PlatformHelper.IsAppleMobile,
            out path);
    }

    /// <summary>
    /// 如果值是持久导入文件引用或旧 iOS 容器路径，则解析到当前容器；否则原样返回。
    /// </summary>
    public static string Resolve(string? reference) =>
        TryResolve(reference, out var path) ? path : reference ?? string.Empty;

    /// <summary>
    /// 获取引用所属的顶层导入项目录名称。
    /// </summary>
    public static bool TryGetItemDirectoryName(
        string? reference,
        out string directoryName)
    {
        return PortableImportedFileReference.TryGetItemDirectoryName(
            reference,
            PlatformHelper.IsAppleMobile,
            out directoryName);
    }
}
