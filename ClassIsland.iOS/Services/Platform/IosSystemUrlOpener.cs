using Foundation;
using UIKit;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// 在主线程上调用 iOS 系统 URL 启动接口。
/// </summary>
internal static class IosSystemUrlOpener
{
    public static Task<bool> OpenAsync(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri)
        {
            throw new ArgumentException("只能打开绝对 URI。", nameof(uri));
        }

        var completionSource = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        UIApplication.SharedApplication.BeginInvokeOnMainThread(() =>
        {
            try
            {
                using var url = new NSUrl(uri.AbsoluteUri);
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
