using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace ClassIsland;

public partial class MainWindow
{
    /// <summary>
    /// 样式请求的主界面可见状态，实际隐藏会等待退场动画结束。
    /// </summary>
    public static readonly AttachedProperty<bool> IsContentVisibleRequestedProperty =
        AvaloniaProperty.RegisterAttached<MainWindow, Control, bool>("IsContentVisibleRequested", true);

    /// <summary>
    /// 获取样式请求的主界面可见状态。
    /// </summary>
    public static bool GetIsContentVisibleRequested(Control control) => control.GetValue(IsContentVisibleRequestedProperty);

    /// <summary>
    /// 设置主界面可见状态请求，由主窗口协调过渡动画与实际可见性。
    /// </summary>
    public static void SetIsContentVisibleRequested(Control control, bool value) =>
        control.SetValue(IsContentVisibleRequestedProperty, value);

    private static readonly TimeSpan ContentVisibilityDuration = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan ContentCompositionTimeout = TimeSpan.FromSeconds(2);
    private static readonly Easing ContentEntranceEasing = Easing.Parse("0.25, 1, 0.5, 1");
    private static readonly Easing ContentExitEasing = Easing.Parse("0.4, 0, 1, 1");

    private CompositionVisual? _mainContentVisual;
    private ImplicitAnimationCollection? _previousContentImplicitAnimations;
    private CompositionBatch? _contentAnimationBatch;
    private CancellationTokenSource? _contentAnimationCancellation;
    private long _contentAnimationGeneration;
    private bool _contentVisibilityReady;
    private bool _preparingContentEntrance;
    private bool _contentVisibilityAnimating;
    private bool? _contentVisibilityTarget;

    private bool IsContentVisibilityRequested => ViewModel.IsEditMode || GetIsContentVisibleRequested(GridRoot);

    private void InitializeContentVisibilityAnimation()
    {
        GridRoot.PropertyChanged += OnContentVisibilityPropertyChanged;
        GridRoot.AttachedToVisualTree += OnContentVisibilityAttached;
        GridRoot.DetachedFromVisualTree += OnContentVisibilityDetached;
        SynchronizeContentVisibility();
    }

    private void OnContentVisibilityAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        GridRoot.PropertyChanged -= OnContentVisibilityPropertyChanged;
        GridRoot.PropertyChanged += OnContentVisibilityPropertyChanged;
        PropertyChanged += OnContentAnimationWindowGeometryChanged;
        PositionChanged += OnContentAnimationScreenChanged;
        ScalingChanged += OnContentAnimationScreenChanged;
        SynchronizeContentVisibility();
    }

    private void OnContentVisibilityDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _contentVisibilityReady = false;
        SetContentVisibilityImmediately(IsContentVisibilityRequested);
        GridRoot.PropertyChanged -= OnContentVisibilityPropertyChanged;
        PropertyChanged -= OnContentAnimationWindowGeometryChanged;
        PositionChanged -= OnContentAnimationScreenChanged;
        ScalingChanged -= OnContentAnimationScreenChanged;
        _mainContentVisual = null;
        _previousContentImplicitAnimations = null;
    }

    private void StartInitialContentVisibilityAnimation()
    {
        _contentVisibilityReady = true;
        _contentVisibilityTarget = null;
        // The constructor keeps the first frame transparent while layout is being established.
        _preparingContentEntrance = GridRoot.Opacity == 0;
        UpdateContentVisibility();
    }

    private void OnContentVisibilityPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsContentVisibleRequestedProperty)
        {
            UpdateContentVisibility();
        }
    }

    private void OnContentAnimationWindowGeometryChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_contentVisibilityAnimating && e.Property == BoundsProperty)
        {
            SynchronizeContentVisibility();
        }
    }

    private void OnContentAnimationScreenChanged(object? sender, EventArgs e)
    {
        if (_contentVisibilityAnimating)
        {
            SynchronizeContentVisibility();
        }
    }

    private void OnContentAnimationSettingsChanged(string? propertyName)
    {
        if (propertyName is nameof(ViewModel.Settings.WindowDockingLocation)
            or nameof(ViewModel.Settings.WindowDockingMonitorIndex)
            or nameof(ViewModel.Settings.WindowDockingOffsetX)
            or nameof(ViewModel.Settings.WindowDockingOffsetY)
            or nameof(ViewModel.Settings.IsIgnoreWorkAreaEnabled)
            or nameof(ViewModel.Settings.Scale))
        {
            SynchronizeContentVisibility();
        }
    }

    private void SynchronizeContentVisibility()
    {
        SetContentVisibilityImmediately(IsContentVisibilityRequested);
        if (!_contentVisibilityReady && IThemeService.AnimationLevel >= 2 && !ViewModel.IsEditMode)
        {
            GridRoot.Opacity = 0;
        }
    }

    private void UpdateContentVisibility()
    {
        var visible = IsContentVisibilityRequested;
        if (!_contentVisibilityReady)
        {
            SynchronizeContentVisibility();
            return;
        }
        if (_contentVisibilityTarget == visible)
        {
            return;
        }

        var entrance = visible && (!GridRoot.IsVisible || _preparingContentEntrance);
        if (IThemeService.AnimationLevel < 2 || ViewModel.IsEditMode ||
            (!visible && (!GridRoot.IsVisible || _preparingContentEntrance)))
        {
            SetContentVisibilityImmediately(visible);
            return;
        }

        var visual = ElementComposition.GetElementVisual(GridRoot);
        if (visual == null)
        {
            SetContentVisibilityImmediately(visible);
            return;
        }
        if (_mainContentVisual != visual)
        {
            _mainContentVisual = visual;
            _previousContentImplicitAnimations = visual.ImplicitAnimations;
        }

        CancelContentVisibilityTransition();
        _contentVisibilityTarget = visible;
        GridRoot.IsHitTestVisible = visible;
        var generation = _contentAnimationGeneration;
        if (entrance)
        {
            _preparingContentEntrance = true;
            GridRoot.IsVisible = true;
            GridRoot.Opacity = 1;
            GridRoot.UpdateLayout();
            UpdateWindowPos();
        }

        // Start after layout and Avalonia's visual synchronization so neither can overwrite
        // the initial composition values before the first animated frame is submitted.
        visual.Compositor.RequestCompositionUpdate(() =>
        {
            if (generation != _contentAnimationGeneration || !_contentVisibilityReady)
            {
                return;
            }
            if (GridRoot.Bounds.Height <= 0)
            {
                SetContentVisibilityImmediately(visible);
                return;
            }

            var offset = GetContentHiddenTranslation();
            _preparingContentEntrance = false;
            _contentVisibilityAnimating = true;
            if (entrance)
            {
                ResetContentCompositionVisual(visual);
                var translation = visual.Compositor.CreateVector3DKeyFrameAnimation();
                translation.Target = nameof(CompositionVisual.Translation);
                translation.Duration = ContentVisibilityDuration;
                translation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
                translation.InsertKeyFrame(0, offset);
                translation.InsertKeyFrame(1, new Vector3D(), ContentEntranceEasing);
                var opacity = visual.Compositor.CreateScalarKeyFrameAnimation();
                opacity.Target = nameof(CompositionVisual.Opacity);
                opacity.Duration = ContentVisibilityDuration;
                opacity.StopBehavior = AnimationStopBehavior.SetToFinalValue;
                opacity.InsertKeyFrame(0, 0);
                opacity.InsertKeyFrame(1, 1, ContentEntranceEasing);
                visual.StartAnimation(nameof(CompositionVisual.Translation), translation);
                visual.StartAnimation(nameof(CompositionVisual.Opacity), opacity);
            }
            else
            {
                InstallContentImplicitAnimations(visual, visible ? ContentEntranceEasing : ContentExitEasing);
                visual.Translation = visible ? new Vector3D() : offset;
                visual.Opacity = visible ? 1 : 0;
            }

            _contentAnimationBatch = visual.Compositor.RequestCompositionBatchCommitAsync();
            var cancellation = _contentAnimationCancellation = new CancellationTokenSource();
            _ = CompleteContentVisibilityTransitionAsync(visible, generation, _contentAnimationBatch, cancellation);
        });
    }

    private Vector3D GetContentHiddenTranslation()
    {
        var transform = GridRoot.TransformToVisual(this);
        var top = transform?.Transform(new Point());
        var bottom = transform?.Transform(new Point(0, GridRoot.Bounds.Height));
        var scale = Math.Abs((bottom?.Y - top?.Y) / GridRoot.Bounds.Height ?? ViewModel.Settings.Scale);
        if (!double.IsFinite(scale) || scale <= 0)
        {
            scale = 1;
        }
        var dockingTop = ViewModel.Settings.WindowDockingLocation is 0 or 1 or 2;
        // Coordinates are DIPs. Dividing the distance by the layout scale expresses it
        // in GridRoot's local space without applying the monitor DPI a second time.
        var distance = dockingTop
            ? Math.Max(bottom?.Y ?? GridRoot.Bounds.Height * scale, 0)
            : Math.Max(Bounds.Height - (top?.Y ?? 0), 0);
        return new Vector3D(0, (dockingTop ? -1 : 1) * (distance / scale + 1), 0);
    }

    private void InstallContentImplicitAnimations(CompositionVisual visual, Easing easing)
    {
        var animations = visual.Compositor.CreateImplicitAnimationCollection();
        var translation = visual.Compositor.CreateVector3DKeyFrameAnimation();
        translation.Target = nameof(CompositionVisual.Translation);
        translation.Duration = ContentVisibilityDuration;
        translation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
        translation.InsertExpressionKeyFrame(1, "this.FinalValue", easing);
        animations[nameof(CompositionVisual.Translation)] = translation;
        var opacity = visual.Compositor.CreateScalarKeyFrameAnimation();
        opacity.Target = nameof(CompositionVisual.Opacity);
        opacity.Duration = ContentVisibilityDuration;
        opacity.StopBehavior = AnimationStopBehavior.SetToFinalValue;
        opacity.InsertExpressionKeyFrame(1, "this.FinalValue", easing);
        animations[nameof(CompositionVisual.Opacity)] = opacity;
        visual.ImplicitAnimations = animations;
    }

    private async Task CompleteContentVisibilityTransitionAsync(bool visible, long generation,
        CompositionBatch batch, CancellationTokenSource cancellation)
    {
        var token = cancellation.Token;
        try
        {
            await batch.Processed.WaitAsync(ContentCompositionTimeout, token);
            await Task.Delay(ContentVisibilityDuration, token);
            // Let the terminal frame render before collapsing the layout and its native window.
            if (_mainContentVisual != null)
            {
                await _mainContentVisual.Compositor.RequestCompositionBatchCommitAsync()
                    .Rendered.WaitAsync(ContentCompositionTimeout, token);
            }
            if (generation != _contentAnimationGeneration)
            {
                return;
            }
            _contentVisibilityAnimating = false;
            if (!visible)
            {
                SetContentVisibilityImmediately(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "主界面过渡动画未完成，已同步显示状态。");
            if (generation == _contentAnimationGeneration)
            {
                SetContentVisibilityImmediately(IsContentVisibilityRequested);
            }
        }
        finally
        {
            if (_contentAnimationCancellation == cancellation)
            {
                _contentAnimationCancellation = null;
                cancellation.Dispose();
            }
        }
    }

    private void CancelContentVisibilityTransition()
    {
        ++_contentAnimationGeneration;
        _contentAnimationCancellation?.Cancel();
        _contentAnimationCancellation?.Dispose();
        _contentAnimationCancellation = null;
        _preparingContentEntrance = false;
        _contentVisibilityAnimating = false;
    }

    private void SetContentVisibilityImmediately(bool visible)
    {
        CancelContentVisibilityTransition();
        _contentVisibilityTarget = visible;
        GridRoot.IsVisible = visible;
        GridRoot.IsHitTestVisible = visible;
        GridRoot.Opacity = 1;
        var visual = _mainContentVisual;
        if (visual != null)
        {
            ResetContentCompositionVisual(visual);
            var generation = _contentAnimationGeneration;
            // StopAnimation alone cannot stop an animation whose batch is still in flight.
            // A generation guard prevents this late cleanup from stopping a newer transition.
            _contentAnimationBatch?.Processed.ContinueWith(_ => Dispatcher.UIThread.Post(() =>
            {
                if (generation == _contentAnimationGeneration && _mainContentVisual == visual)
                {
                    ResetContentCompositionVisual(visual);
                }
            }), TaskScheduler.Default);
        }
        _contentAnimationBatch = null;
    }

    private void ResetContentCompositionVisual(CompositionVisual visual)
    {
        visual.ImplicitAnimations = null;
        // Force serialization even when the local base values already equal the resting state,
        // recalling any animation start that has not yet reached the render thread.
        visual.Translation = new Vector3D(1, 0, 0);
        visual.Translation = new Vector3D();
        visual.Opacity = 0;
        visual.Opacity = 1;
        visual.StopAnimation(nameof(CompositionVisual.Translation));
        visual.StopAnimation(nameof(CompositionVisual.Opacity));
        visual.ImplicitAnimations = _previousContentImplicitAnimations;
    }
}
