using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;
using ClassIsland.Core.Abstractions.Services;

namespace ClassIsland.Core.Behaviors;

/// <summary>
/// Popup 进入动画行为
/// </summary>
public class PopupIntroAnimationBehavior
{
    public static readonly AttachedProperty<bool> IsIntroAnimationEnabledProperty =
        AvaloniaProperty.RegisterAttached<PopupIntroAnimationBehavior, PopupRoot, bool>("IsIntroAnimationEnabled");

    public static void SetIsIntroAnimationEnabled(PopupRoot obj, bool value) => obj.SetValue(IsIntroAnimationEnabledProperty, value);
    public static bool GetIsIntroAnimationEnabled(PopupRoot obj) => obj.GetValue(IsIntroAnimationEnabledProperty);

    static PopupIntroAnimationBehavior()
    {
        IsIntroAnimationEnabledProperty.Changed.AddClassHandler<PopupRoot>(IsIntroAnimationEnabledChanged);
    }

    private static void IsIntroAnimationEnabledChanged(PopupRoot control, AvaloniaPropertyChangedEventArgs args)
    {
        if (!GetIsIntroAnimationEnabled(control))
        {
            return;
        }
        
        control.Opened += ControlOnOpened;
    }

    private static void ControlOnOpened(object? sender, EventArgs e)
    {
        if (sender is not PopupRoot control)
        {
            return;
        }

        control.Opened -= ControlOnOpened;
        var visual = ElementComposition.GetElementVisual(control);
        if (visual == null)
        {
            return;
        }

        if (IThemeService.AnimationLevel < 1)
        {
            return;
        }
        var compositor = visual.Compositor;
        var popup = control.Parent as Popup;
        var animationOpacity = compositor.CreateScalarKeyFrameAnimation();
        animationOpacity.Target = nameof(visual.Opacity);
        animationOpacity.Duration = TimeSpan.FromSeconds(0.15);
        animationOpacity.InsertKeyFrame(0f, 0f);
        animationOpacity.InsertKeyFrame(1f, 1f, Easing.Parse("0.22, 1, 0.36, 1"));
        visual.StartAnimation(nameof(visual.Opacity), animationOpacity);
        
        var origin = GetOriginFromPlacement(popup?.Placement ?? PlacementMode.Pointer, control.Bounds, visual.CenterPoint);
        visual.CenterPoint = origin;
        var animationScale = compositor.CreateVector3DKeyFrameAnimation();
        animationScale.Target = nameof(visual.Scale);
        animationScale.Duration = TimeSpan.FromSeconds(0.15);
        animationScale.InsertKeyFrame(0f, visual.Scale with { X = 0.925, Y = 0.925 });
        animationScale.InsertKeyFrame(1f, visual.Scale with { X = 1, Y = 1 }, Easing.Parse("0.22, 1, 0.36, 1"));
        visual.StartAnimation(nameof(visual.Scale), animationScale);
    }

    private static Vector3D GetOriginFromPlacement(PlacementMode p, Rect size, Vector3D vectorRaw)
    {
        var relative = p switch
        {
            PlacementMode.Bottom => new RelativePoint(0.5, 0.0, RelativeUnit.Relative),
            PlacementMode.Left => new RelativePoint(1.0, 0.5, RelativeUnit.Relative),
            PlacementMode.Right => new RelativePoint(0.0, 0.5, RelativeUnit.Relative),
            PlacementMode.Top => new RelativePoint(0.5, 1.0, RelativeUnit.Relative),
            PlacementMode.Pointer => new RelativePoint(0.0, 0.0, RelativeUnit.Relative),
            PlacementMode.Center or PlacementMode.AnchorAndGravity =>
                new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            PlacementMode.BottomEdgeAlignedLeft => new RelativePoint(0.0, 0.0, RelativeUnit.Relative),
            PlacementMode.BottomEdgeAlignedRight => new RelativePoint(1.0, 0.0, RelativeUnit.Relative),
            PlacementMode.LeftEdgeAlignedTop => new RelativePoint(1.0, 1.0, RelativeUnit.Relative),
            PlacementMode.LeftEdgeAlignedBottom => new RelativePoint(1.0, 0.0, RelativeUnit.Relative),
            PlacementMode.RightEdgeAlignedTop => new RelativePoint(0.0, 1.0, RelativeUnit.Relative),
            PlacementMode.RightEdgeAlignedBottom => new RelativePoint(0.0, 0.0, RelativeUnit.Relative),
            PlacementMode.TopEdgeAlignedLeft => new RelativePoint(0.0, 1.0, RelativeUnit.Relative),
            PlacementMode.TopEdgeAlignedRight => new RelativePoint(1.0, 1.0, RelativeUnit.Relative),
            _ => new RelativePoint(0.5, 0.5, RelativeUnit.Relative)
        };
        return vectorRaw with { X = size.Width * relative.Point.X, Y = size.Height * relative.Point.Y };
    }
}