using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// 使用 iOS/iPadOS 系统浏览器打开外部 URI。
/// </summary>
internal sealed class IosPlatformUriLauncherService : IPlatformUriLauncherService
{
    public Task<bool> OpenUriAsync(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri)
        {
            throw new ArgumentException("只能打开绝对 URI。", nameof(uri));
        }

        return IosSystemUrlOpener.OpenAsync(uri);
    }
}
