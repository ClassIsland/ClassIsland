using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.VisualTree;

namespace ClassIsland.Core.Controls;

/// <summary>
/// A composition-backed page transition that visually matches the
/// <c>PageSlide</c> + <c>CrossFade</c> composite transition: the outgoing page
/// slides horizontally out while fading out, and the incoming page slides in
/// from the opposite side while fading in.
/// </summary>
public class CompositionPageTransition : IPageTransition
{
    /// <summary>
    /// Duration of both the slide and fade animations.
    /// </summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Easing applied to the outgoing page's slide animation.
    /// </summary>
    public Easing SlideOutEasing { get; set; } = Easing.Parse("0,0 0,1");

    /// <summary>
    /// Easing applied to the incoming page's slide animation.
    /// </summary>
    public Easing SlideInEasing { get; set; } = Easing.Parse("0,0 0,1");

    /// <summary>
    /// Easing applied to the outgoing page's fade animation.
    /// </summary>
    public Easing FadeOutEasing { get; set; } = Easing.Parse("0,0 0,1");

    /// <summary>
    /// Easing applied to the incoming page's fade animation.
    /// </summary>
    public Easing FadeInEasing { get; set; } = Easing.Parse("0,0 0,1");

    /// <inheritdoc />
    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (from is null || to is null)
        {
            if (from is not null)
            {
                from.IsVisible = false;
            }

            if (to is not null)
            {
                to.IsVisible = true;
            }

            return;
        }

        var fromCompositionVisual = ElementComposition.GetElementVisual(from);
        var toCompositionVisual = ElementComposition.GetElementVisual(to);

        if (fromCompositionVisual is null || toCompositionVisual is null)
        {
            // Composition isn't available for one of the pages; snap instead of
            // leaving both pages visible and stacked on top of each other.
            from.IsVisible = false;
            to.IsVisible = true;
            return;
        }

        var parent = from.GetVisualParent() ?? to.GetVisualParent();
        var distance = parent?.Bounds.Width
                       ?? Math.Max(from.Bounds.Width, to.Bounds.Width);

        if (distance <= 0)
        {
            from.IsVisible = false;
            to.IsVisible = true;
            return;
        }

        var fromBaseTranslation = fromCompositionVisual.Translation;
        var toBaseTranslation = toCompositionVisual.Translation;
        var fromBaseOpacity = fromCompositionVisual.Opacity;
        var toBaseOpacity = toCompositionVisual.Opacity;

        // The incoming page is shown by TabControl before Start is called, but
        // mirror PageSlide and make it visible here as well for standalone use.
        to.IsVisible = true;

        // PageSlide replaces the render transform with a translation starting at
        // zero, so start from zero instead of the visual's base translation.
        StartSlideAnimation(
            fromCompositionVisual,
            new Vector3D(),
            new Vector3D(forward ? -distance : distance, 0, 0),
            SlideOutEasing);

        StartSlideAnimation(
            toCompositionVisual,
            new Vector3D(forward ? distance : -distance, 0, 0),
            new Vector3D(),
            SlideInEasing);

        // CrossFade drives Opacity from 1 to 0 on the outgoing page and from
        // 0 to 1 on the incoming page.
        StartFadeAnimation(fromCompositionVisual, 1f, 0f, FadeOutEasing);
        StartFadeAnimation(toCompositionVisual, 0f, 1f, FadeInEasing);

        try
        {
            await Task.Delay(Duration, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            RestoreVisual(fromCompositionVisual, fromBaseTranslation, fromBaseOpacity);
            RestoreVisual(toCompositionVisual, toBaseTranslation, toBaseOpacity);
            return;
        }

        if (!cancellationToken.IsCancellationRequested)
        {
            from.IsVisible = false;
        }

        // TabControl clears and swaps the presenters immediately after Start
        // completes. Stop the composition animations here and leave the visuals
        // at their base values so stale transforms can't survive re-attaching.
        RestoreVisual(fromCompositionVisual, fromBaseTranslation, fromBaseOpacity);
        RestoreVisual(toCompositionVisual, toBaseTranslation, toBaseOpacity);
    }

    private void StartSlideAnimation(
        CompositionVisual visual,
        Vector3D from,
        Vector3D to,
        Easing easing)
    {
        var animation = visual.Compositor.CreateVector3DKeyFrameAnimation();
        animation.Target = nameof(CompositionVisual.Translation);
        animation.Duration = Duration;
        animation.InsertKeyFrame(0f, from);
        animation.InsertKeyFrame(1f, to, easing);
        visual.StartAnimation(nameof(CompositionVisual.Translation), animation);
    }

    private void StartFadeAnimation(
        CompositionVisual visual,
        float from,
        float to,
        Easing easing)
    {
        var animation = visual.Compositor.CreateScalarKeyFrameAnimation();
        animation.Target = nameof(CompositionVisual.Opacity);
        animation.Duration = Duration;
        animation.InsertKeyFrame(0f, from);
        animation.InsertKeyFrame(1f, to, easing);
        visual.StartAnimation(nameof(CompositionVisual.Opacity), animation);
    }

    private static void RestoreVisual(
        CompositionVisual visual,
        Vector3D translation,
        float opacity)
    {
        visual.StopAnimation(nameof(CompositionVisual.Translation));
        visual.StopAnimation(nameof(CompositionVisual.Opacity));
        visual.Translation = translation;
        visual.Opacity = opacity;
    }
}
