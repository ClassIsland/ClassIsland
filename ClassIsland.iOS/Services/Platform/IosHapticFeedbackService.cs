using Avalonia.Threading;
using ClassIsland.Platforms.Abstraction.Services;
using UIKit;

namespace ClassIsland.iOS.Services.Platform;

internal sealed class IosHapticFeedbackService : IHapticFeedbackService
{
    public void PlayTick() => Dispatcher.UIThread.Post(() =>
    {
        if (UIApplication.SharedApplication.ApplicationState != UIApplicationState.Active)
        {
            return;
        }

        using var feedback = new UISelectionFeedbackGenerator();
        feedback.SelectionChanged();
    });

    public void PlayScrollBoundary() => Dispatcher.UIThread.Post(() =>
    {
        if (UIApplication.SharedApplication.ApplicationState != UIApplicationState.Active)
        {
            return;
        }

        using var feedback = new UIImpactFeedbackGenerator(UIImpactFeedbackStyle.Soft);
        feedback.ImpactOccurred();
    });
}
