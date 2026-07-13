using ClassIsland.Platforms.Abstraction.Services;
using Foundation;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// 使用 iOS/iPadOS“文件”App 显示应用 Documents 中的目录。
/// </summary>
internal sealed class IosPlatformFolderService : IPlatformFolderService
{
    private readonly SharedDocumentsPlatformFolderService _service = new(
        GetDocumentsPath,
        OpenFilesAppAsync);

    public Task<bool> OpenFolderAsync(string folderPath) => _service.OpenFolderAsync(folderPath);

    private static string GetDocumentsPath()
    {
        var documentsUrl = NSFileManager.DefaultManager
            .GetUrls(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("无法获取 iOS Documents 目录。");
        return documentsUrl.Path
               ?? throw new InvalidOperationException("无法获取 iOS Documents 目录路径。");
    }

    private static Task<bool> OpenFilesAppAsync(Uri uri) => IosSystemUrlOpener.OpenAsync(uri);
}
