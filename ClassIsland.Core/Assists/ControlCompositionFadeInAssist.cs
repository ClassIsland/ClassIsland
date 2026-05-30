using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;

namespace ClassIsland.Core.Assists;

public class ControlCompositionFadeInAssist
{
    public static readonly AttachedProperty<bool> IsFadeInEnabledProperty =
        AvaloniaProperty.RegisterAttached<ControlCompositionFadeInAssist, Visual, bool>("IsFadeInEnabled");

    public static void SetIsFadeInEnabled(Visual obj, bool value) => obj.SetValue(IsFadeInEnabledProperty, value);
    public static bool GetIsFadeInEnabled(Visual obj) => obj.GetValue(IsFadeInEnabledProperty);

    static ControlCompositionFadeInAssist()
    {
        IsFadeInEnabledProperty.Changed.AddClassHandler<Visual>(HandleIsFadeInEnabledChanged);
    }

    private static void HandleIsFadeInEnabledChanged(Visual visual, AvaloniaPropertyChangedEventArgs args)
    {
        visual.AttachedToVisualTree -= VisualOnAttachedToVisualTree;
        if (GetIsFadeInEnabled(visual))
        {
            visual.AttachedToVisualTree += VisualOnAttachedToVisualTree;
        }
    }

    private static void VisualOnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not Visual visual)
        {
            return;
        }

        var compositionVisual = ElementComposition.GetElementVisual(visual);
        if (compositionVisual == null)
        {
            return;
        }

        var compositor = compositionVisual.Compositor;
        var animation = compositor.CreateScalarKeyFrameAnimation();
        animation.Target = nameof(compositionVisual.Opacity);
        animation.Duration = TimeSpan.FromMilliseconds(200);
        animation.InsertKeyFrame(0f, 0f);
        animation.InsertKeyFrame(1f, 1f, Easing.Parse("0.25, 1, 0.5, 1"));
        compositionVisual.StartAnimation(nameof(compositionVisual.Opacity), animation);
    }
}
