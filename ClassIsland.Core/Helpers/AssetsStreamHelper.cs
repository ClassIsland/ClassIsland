using Avalonia.Platform;

namespace ClassIsland.Core.Helpers;

/// <summary>
/// 资源读取辅助类
/// </summary>
public static class AssetsStreamHelper
{
    /// <summary>
    /// 同步读取某个资源的所有文本。
    /// </summary>
    /// <param name="uri">资源 Uri。</param>
    /// <returns>资源包含的文本。</returns>
    public static string ReadAllTextFromAsset(Uri uri)
    {
        using var sr = new StreamReader(AssetLoader.Open(uri));
        return sr.ReadToEnd();
    }

    /// <summary>
    /// 读取某个资源的所有文本
    /// </summary>
    /// <param name="uri">资源 Uri</param>
    /// <returns>资源包含的文本</returns>
    public static async Task<string> ReadAllTextFromAssetAsync(Uri uri)
    {
        using var sr = new StreamReader(AssetLoader.Open(uri));
        return await sr.ReadToEndAsync().ConfigureAwait(false);
    }
}
