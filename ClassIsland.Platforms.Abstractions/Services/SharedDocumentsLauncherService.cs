namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 将 Documents 内的目录转换为 Apple“文件”App 可识别的 URL。
/// </summary>
internal sealed class SharedDocumentsLauncherService(
    Func<string> documentsPathProvider,
    Func<Uri, Task<bool>> openUriAsync) : ILauncherService
{
    public Task LaunchPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
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

        return OpenRequiredAsync(
            CreateFilesAppUri(fullPath),
            $"“文件”App 无法打开目录：{fullPath}");
    }

    public Task LaunchUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("只能打开绝对 URL。", nameof(url));
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "iOS/iPadOS 仅支持通过 Safari 打开 HTTP(S) 链接。",
                nameof(url));
        }

        return OpenRequiredAsync(uri, $"系统无法打开链接：{uri}");
    }

    public Task LaunchAppLink(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme is "file" or "javascript" or "data" or "about" or "blob" or "shareddocuments")
        {
            throw new ArgumentException("请输入目标 App 提供的完整链接，不能使用文件路径。", nameof(url));
        }

        return OpenRequiredAsync(uri, "无法打开 App 链接，请确认已安装目标 App，并检查链接是否正确。");
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

    private async Task OpenRequiredAsync(Uri uri, string failureMessage)
    {
        bool opened;
        try
        {
            opened = await openUriAsync(uri).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(failureMessage, exception);
        }

        if (!opened)
        {
            throw new InvalidOperationException(failureMessage);
        }
    }

    private static bool IsPathWithinDirectory(string path, string directory)
    {
        var relativePath = Path.GetRelativePath(directory, path);
        return !Path.IsPathRooted(relativePath)
               && relativePath != ".."
               && !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }
}
