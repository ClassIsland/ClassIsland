using ClassIsland.Core.Helpers;
using ClassIsland.Platforms.Abstraction.Services;
using Foundation;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// 使用 iOS/iPadOS“文件”App 打开应用目录，并使用系统 URL opener 打开外部链接。
/// </summary>
internal sealed class IosLauncherService : ILauncherService
{
    private readonly SharedDocumentsLauncherService _service = new(
        GetDocumentsPath,
        IosSystemUrlOpener.OpenAsync);

    public Task LaunchPath(string path)
    {
        path = ImportedFileReference.Resolve(path);
        if (File.Exists(path))
        {
            path = Path.GetDirectoryName(path)
                   ?? throw new DirectoryNotFoundException(
                       "无法获取所选文件的父目录。");
        }

        return _service.LaunchPath(path);
    }

    public Task LaunchUrl(string url) => _service.LaunchUrl(url);

    private static string GetDocumentsPath()
    {
        var documentsUrl = NSFileManager.DefaultManager
            .GetUrls(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("无法获取 iOS Documents 目录。");
        return documentsUrl.Path
               ?? throw new InvalidOperationException("无法获取 iOS Documents 目录路径。");
    }
}
