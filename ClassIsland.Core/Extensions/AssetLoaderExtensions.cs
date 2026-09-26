using Avalonia.Platform;
using ClassIsland.Core.Helpers;

namespace ClassIsland.Core.Extensions;

/// <summary>
/// <see cref="AssetLoader"/> 的扩展方法。
/// </summary>
public static class AssetLoaderExtensions
{
    extension(AssetLoader)
    {
        /// <summary>
        /// 同步读取指定资源中的全部文本。
        /// </summary>
        /// <param name="uri">要读取的资源 URI。</param>
        /// <returns>资源的全部文本。</returns>
        public static string ReadAllText(Uri uri) =>
            AssetsStreamHelper.ReadAllTextFromAsset(uri);

        /// <summary>
        /// 异步读取指定资源中的全部文本。
        /// </summary>
        /// <param name="uri">要读取的资源 URI。</param>
        /// <returns>包含资源全部文本的异步操作。</returns>
        public static Task<string> ReadAllTextAsync(Uri uri) =>
            AssetsStreamHelper.ReadAllTextFromAssetAsync(uri);
    }
}
