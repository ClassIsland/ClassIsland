using Android.Views;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Android.Services.Platform;

public class HapticFeedbackService : IHapticFeedbackService
{
    public void PlayTick()
    {
        if (MainActivity.Current?.TryGetTarget(out var activity) != true ||
            activity == null || !activity.IsForeground || activity.IsFinishing || activity.IsDestroyed)
            return;

        activity.Window?.DecorView?.PerformHapticFeedback(FeedbackConstants.ClockTick);
    }
}
