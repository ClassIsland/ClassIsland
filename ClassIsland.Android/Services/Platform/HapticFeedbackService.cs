using Android.Views;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Android.Services.Platform;

public class HapticFeedbackService : IHapticFeedbackService
{
    public void PlayTick() => PlayFeedback(FeedbackConstants.ClockTick);

    public void PlayScrollBoundary() => PlayFeedback(OperatingSystem.IsAndroidVersionAtLeast(27)
        ? FeedbackConstants.TextHandleMove
        : FeedbackConstants.ClockTick);

    private static void PlayFeedback(FeedbackConstants feedback)
    {
        if (MainActivity.Current?.TryGetTarget(out var activity) != true ||
            activity == null || !activity.IsForeground || activity.IsFinishing || activity.IsDestroyed)
            return;

        activity.Window?.DecorView?.PerformHapticFeedback(feedback);
    }
}
