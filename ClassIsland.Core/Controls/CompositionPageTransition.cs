using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ClassIsland.Core.Controls;

/// <summary>
/// A composition-backed page transition that visually matches the
/// <c>PageSlide</c> + <c>CrossFade</c> composite transition: the outgoing page
/// slides horizontally out while fading out, and the incoming page slides in
/// from the opposite side while fading in.
/// </summary>
/// <remarks>
/// Composition animations are not driven by the UI thread: they are serialized
/// into a composition batch and applied by the render thread later, so under
/// heavy load the batch that carries <c>StartAnimation</c> can reach the
/// compositor after this method already tried to stop the animations. Such a
/// late-applied animation would otherwise stay attached to the presenter visual
/// forever and shadow every explicit property assignment (TabControl reuses the
/// same two presenters for all tab switches). Cleanup therefore assigns the rest
/// values through a distinct temporary value first (which recalls animations
/// that were not committed yet), stops live animations immediately, and queues
/// an extra generation-guarded pass once the render thread confirms the batch,
/// so animations applied late are stopped as well. The generation guard also
/// prevents a cancelled transition from stopping a newer transition's
/// animations on the same presenters.
/// </remarks>
public class CompositionPageTransition : IPageTransition
{
    private static readonly ConditionalWeakTable<CompositionVisual, GenerationBox> VisualGenerations = new();

    private static long _currentGeneration;

    /// <summary>
    /// Upper bound for waiting until the render thread confirms that the
    /// transition animations have been applied; afterwards cleanup falls back to
    /// best effort and the post-batch pass finishes the job.
    /// </summary>
    private static readonly TimeSpan ApplyConfirmationTimeout = TimeSpan.FromSeconds(2);

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

        // Claim both visuals for this transition so cleanup of an older,
        // still-running transition can never touch them while they are reused
        // by a newer one (TabControl keeps only two presenter visuals alive).
        var generation = Interlocked.Increment(ref _currentGeneration);
        ClaimGeneration(fromCompositionVisual, generation);
        ClaimGeneration(toCompositionVisual, generation);

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

        // The animations ride in the composition batch that is current right now;
        // keep a handle to it so cleanup can wait until the render thread has
        // actually applied them before stopping anything.
        CompositionBatch? batch = null;
        try
        {
            batch = fromCompositionVisual.Compositor.RequestCompositionBatchCommitAsync();
        }
        catch
        {
            // Batch tracking only improves timing precision; the cleanup paths
            // below stay correct without it.
        }

        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            if (batch is not null)
            {
                try
                {
                    await batch.Processed.WaitAsync(ApplyConfirmationTimeout, cancellationToken);
                }
                catch (TimeoutException)
                {
                    // The compositor is extremely backed up; stop waiting and let
                    // the post-batch cleanup deal with animations applied later.
                }
            }

            // The animation timeline starts when the batch was committed, so only
            // the remainder of the duration has to elapse after confirmation.
            var remaining = Duration - Stopwatch.GetElapsedTime(startedAt);
            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            RestoreVisuals(
                generation,
                fromCompositionVisual, fromBaseTranslation, fromBaseOpacity,
                toCompositionVisual, toBaseTranslation, toBaseOpacity);
            QueuePostApplyCleanup(
                batch, generation,
                fromCompositionVisual, fromBaseTranslation, fromBaseOpacity,
                toCompositionVisual, toBaseTranslation, toBaseOpacity);
            return;
        }

        if (!cancellationToken.IsCancellationRequested)
        {
            from.IsVisible = false;
        }

        RestoreVisuals(
            generation,
            fromCompositionVisual, fromBaseTranslation, fromBaseOpacity,
            toCompositionVisual, toBaseTranslation, toBaseOpacity);
        QueuePostApplyCleanup(
            batch, generation,
            fromCompositionVisual, fromBaseTranslation, fromBaseOpacity,
            toCompositionVisual, toBaseTranslation, toBaseOpacity);
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

    private static void RestoreVisuals(
        long generation,
        CompositionVisual from, Vector3D fromTranslation, float fromOpacity,
        CompositionVisual to, Vector3D toTranslation, float toOpacity)
    {
        RestoreVisualIfOwned(from, generation, fromTranslation, fromOpacity);
        RestoreVisualIfOwned(to, generation, toTranslation, toOpacity);
    }

    private static void RestoreVisualIfOwned(
        CompositionVisual visual,
        long generation,
        Vector3D translation,
        float opacity)
    {
        if (!VisualGenerations.TryGetValue(visual, out var box) || box.Value != generation)
        {
            return;
        }

        // CompositionVisual ignores assignments equal to its local base value,
        // which would leave an uncommitted animation in PendingAnimations. Force
        // each property through a different value so the final assignment is
        // serialized as a direct value and reliably replaces the pending start.
        // Only the final values are serialized, so the temporary values never
        // reach the render thread.
        visual.Translation = new Vector3D(
            translation.X == 0 ? 1 : 0,
            translation.Y,
            translation.Z);
        visual.Translation = translation;
        visual.Opacity = opacity == 0 ? 1 : 0;
        visual.Opacity = opacity;

        // Direct assignments recall starts that haven't been committed yet;
        // StopAnimation handles animations already attached to the compositor.
        visual.StopAnimation(nameof(CompositionVisual.Translation));
        visual.StopAnimation(nameof(CompositionVisual.Opacity));
    }

    private static void QueuePostApplyCleanup(
        CompositionBatch? batch,
        long generation,
        CompositionVisual from, Vector3D fromTranslation, float fromOpacity,
        CompositionVisual to, Vector3D toTranslation, float toOpacity)
    {
        void Cleanup()
        {
            RestoreVisualIfOwned(from, generation, fromTranslation, fromOpacity);
            RestoreVisualIfOwned(to, generation, toTranslation, toOpacity);
        }

        if (batch is null)
        {
            Dispatcher.UIThread.Post(Cleanup, DispatcherPriority.Background);
            return;
        }

        // Runs once the render thread has processed the batch that carried the
        // animation starts; stops issued earlier would silently miss animations
        // that had not been applied yet. Idempotent for the common case where
        // the immediate restore already succeeded.
        batch.Processed.ContinueWith(
            _ => Dispatcher.UIThread.Post(Cleanup, DispatcherPriority.Send),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private static void ClaimGeneration(CompositionVisual visual, long generation)
    {
        VisualGenerations.GetOrCreateValue(visual).Value = generation;
    }

    private sealed class GenerationBox
    {
        public long Value;
    }
}
