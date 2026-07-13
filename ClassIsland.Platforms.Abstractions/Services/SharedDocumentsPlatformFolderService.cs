namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 将 Documents 内的目录转换为 Apple“文件”App 可识别的 URL。
/// </summary>
internal sealed class SharedDocumentsPlatformFolderService(
    Func<string> documentsPathProvider,
    Func<Uri, Task<bool>> openUriAsync) : IPlatformFolderService
{
    public Task<bool> OpenFolderAsync(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        var fullPath = Path.GetFullPath(folderPath);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"要打开的目录不存在：{fullPath}");
        }

        var documentsPath = Path.GetFullPath(documentsPathProvider());
        if (!IsPathWithinDirectory(fullPath, documentsPath))
        {
            throw new PlatformNotSupportedException(
                "iOS/iPadOS 的“文件”App 只能显示应用 Documents 目录中的内容。");
        }

        return openUriAsync(CreateFilesAppUri(fullPath));
    }

    internal static Uri CreateFilesAppUri(string fullPath)
    {
        var fileUriBuilder = new UriBuilder
        {
            Scheme = Uri.UriSchemeFile,
            Host = string.Empty,
            Port = -1,
            Path = fullPath
        };
        var filesAppUriText = fileUriBuilder.Uri.AbsoluteUri.Replace(
            "file://",
            "shareddocuments://",
            StringComparison.Ordinal);
        return new Uri(filesAppUriText, UriKind.Absolute);
    }

    private static bool IsPathWithinDirectory(string path, string directory)
    {
        var relativePath = Path.GetRelativePath(directory, path);
        return !Path.IsPathRooted(relativePath)
               && relativePath != ".."
               && !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }
}
