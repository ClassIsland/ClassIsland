using ClassIsland.Platforms.Abstraction.Services;
using Foundation;
using UIKit;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// 使用 iOS/iPadOS“文件”App 显示应用 Documents 中的目录。
/// </summary>
internal sealed class IosPlatformFolderService : IPlatformFolderService
{
    public Task<bool> OpenFolderAsync(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        var fullPath = Path.GetFullPath(folderPath);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"要打开的目录不存在：{fullPath}");
        }

        var documentsUrl = NSFileManager.DefaultManager
            .GetUrls(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("无法获取 iOS Documents 目录。");
        var documentsPath = documentsUrl.Path
            ?? throw new InvalidOperationException("无法获取 iOS Documents 目录路径。");

        if (!IsPathWithinDirectory(fullPath, documentsPath))
        {
            throw new PlatformNotSupportedException(
                "iOS/iPadOS 的“文件”App 只能显示应用 Documents 目录中的内容。");
        }

        var fileUrl = NSUrl.FromFilename(fullPath);
        var filesAppUrlText = fileUrl.AbsoluteString.Replace(
            "file://",
            "shareddocuments://",
            StringComparison.Ordinal);
        var filesAppUrl = new NSUrl(filesAppUrlText);

        return OpenUrlOnMainThreadAsync(filesAppUrl);
    }

    private static bool IsPathWithinDirectory(string path, string directory)
    {
        var relativePath = Path.GetRelativePath(directory, path);
        return !Path.IsPathRooted(relativePath)
               && relativePath != ".."
               && !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static Task<bool> OpenUrlOnMainThreadAsync(NSUrl url)
    {
        var completionSource = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        UIApplication.SharedApplication.BeginInvokeOnMainThread(() =>
        {
            try
            {
                UIApplication.SharedApplication.OpenUrl(
                    url,
                    new UIApplicationOpenUrlOptions(),
                    opened => completionSource.TrySetResult(opened));
            }
            catch (Exception exception)
            {
                completionSource.TrySetException(exception);
            }
        });

        return completionSource.Task;
    }
}
