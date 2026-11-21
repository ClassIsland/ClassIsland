using ClassIsland.Platforms.Abstraction.Services;
using Foundation;
using UIKit;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// 将 iOS 的显著时间和时区变化转发到共享时间服务。
/// </summary>
internal sealed class IosSystemEventsService : ISystemEventsService, IDisposable
{
    private NSObject? _significantTimeChangeObserver;
    private NSObject? _timeZoneChangeObserver;

    public IosSystemEventsService()
    {
        _significantTimeChangeObserver =
            NSNotificationCenter.DefaultCenter.AddObserver(
                UIApplication.SignificantTimeChangeNotification,
                OnTimeChanged);
        _timeZoneChangeObserver =
            NSNotificationCenter.DefaultCenter.AddObserver(
                NSTimeZone.SystemTimeZoneDidChangeNotification,
                OnTimeChanged);
    }

    public event EventHandler? TimeChanged;

    private void OnTimeChanged(NSNotification notification)
    {
        TimeZoneInfo.ClearCachedData();
        TimeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        DisposeObserver(ref _significantTimeChangeObserver);
        DisposeObserver(ref _timeZoneChangeObserver);
    }

    private static void DisposeObserver(ref NSObject? observer)
    {
        if (observer == null)
        {
            return;
        }

        NSNotificationCenter.DefaultCenter.RemoveObserver(observer);
        observer.Dispose();
        observer = null;
    }
}
