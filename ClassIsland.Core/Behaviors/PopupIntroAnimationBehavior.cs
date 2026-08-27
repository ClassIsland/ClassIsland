using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using ClassIsland.Core.Abstractions.Services;

namespace ClassIsland.Core.Behaviors;

/// <summary>
/// Popup 进入动画行为
/// </summary>
public class PopupIntroAnimationBehavior
{
    private const double TranslationDistance = 16;

    private static readonly TimeSpan TranslationDuration = TimeSpan.FromMilliseconds(167);
    private static readonly TimeSpan OpacityDuration = TimeSpan.FromMilliseconds(83);
    private static readonly Easing TranslationEasing = Easing.Parse("0,0 0,1");
    private static readonly Easing OpacityEasing = new LinearEasing();

    public static readonly AttachedProperty<bool> IsIntroAnimationEnabledProperty =
        AvaloniaProperty.RegisterAttached<PopupIntroAnimationBehavior, Control, bool>("IsIntroAnimationEnabled");

    public static void SetIsIntroAnimationEnabled(Control obj, bool value) => obj.SetValue(IsIntroAnimationEnabledProperty, value);
    public static bool GetIsIntroAnimationEnabled(Control obj) => obj.GetValue(IsIntroAnimationEnabledProperty);

    static PopupIntroAnimationBehavior()
    {
        IsIntroAnimationEnabledProperty.Changed.AddClassHandler<Control>(IsIntroAnimationEnabledChanged);
    }

    private static void IsIntroAnimationEnabledChanged(Control control, AvaloniaPropertyChangedEventArgs args)
    {
        if (!GetIsIntroAnimationEnabled(control))
        {
            return;
        }

        switch (control)
        {
            case PopupRoot popupRoot:
                popupRoot.Opened += ControlOnOpened;
                break;
            case OverlayPopupHost overlayPopupHost:
                overlayPopupHost.AttachedToVisualTree += ControlOnOpened;
                break;
        }
    }

    private static void ControlOnOpened(object? sender, EventArgs e)
    {
        if (sender is not Control control)
        {
            return;
        }

        switch (control)
        {
            case PopupRoot popupRoot:
                popupRoot.Opened -= ControlOnOpened;
                break;
            case OverlayPopupHost overlayPopupHost:
                overlayPopupHost.AttachedToVisualTree -= ControlOnOpened;
                break;
        }
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
        var finalOpacity = visual.Opacity;
        var finalTranslation = visual.Translation;

        visual.StopAnimation(nameof(visual.Opacity));
        visual.StopAnimation(nameof(visual.Translation));

        var animationOpacity = compositor.CreateScalarKeyFrameAnimation();
        animationOpacity.Target = nameof(visual.Opacity);
        animationOpacity.Duration = OpacityDuration;
        animationOpacity.StopBehavior = AnimationStopBehavior.SetToFinalValue;
        animationOpacity.InsertKeyFrame(0f, 0f);
        animationOpacity.InsertKeyFrame(1f, finalOpacity, OpacityEasing);
        visual.StartAnimation(nameof(visual.Opacity), animationOpacity);

        var translationOffset = GetTranslationOffset(popup);
        var animationTranslation = compositor.CreateVector3DKeyFrameAnimation();
        animationTranslation.Target = nameof(visual.Translation);
        animationTranslation.Duration = TranslationDuration;
        animationTranslation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
        animationTranslation.InsertKeyFrame(0f, new Vector3D(
            finalTranslation.X + translationOffset.X,
            finalTranslation.Y + translationOffset.Y,
            finalTranslation.Z + translationOffset.Z));
        animationTranslation.InsertKeyFrame(1f, finalTranslation, TranslationEasing);
        visual.StartAnimation(nameof(visual.Translation), animationTranslation);
    }

    private static Vector3D GetTranslationOffset(Popup? popup)
    {
        return popup?.Placement switch
        {
            PlacementMode.Bottom or PlacementMode.BottomEdgeAlignedLeft or PlacementMode.BottomEdgeAlignedRight =>
                new Vector3D(0, -TranslationDistance, 0),
            PlacementMode.Top or PlacementMode.TopEdgeAlignedLeft or PlacementMode.TopEdgeAlignedRight =>
                new Vector3D(0, TranslationDistance, 0),
            PlacementMode.Left or PlacementMode.LeftEdgeAlignedTop or PlacementMode.LeftEdgeAlignedBottom =>
                new Vector3D(TranslationDistance, 0, 0),
            PlacementMode.Right or PlacementMode.RightEdgeAlignedTop or PlacementMode.RightEdgeAlignedBottom =>
                new Vector3D(-TranslationDistance, 0, 0),
            PlacementMode.AnchorAndGravity => GetTranslationOffset(popup.PlacementGravity),
            _ => new Vector3D(0, TranslationDistance, 0)
        };
    }

    private static Vector3D GetTranslationOffset(PopupGravity gravity)
    {
        if ((gravity & PopupGravity.Top) == PopupGravity.Top)
        {
            return new Vector3D(0, TranslationDistance, 0);
        }

        if ((gravity & PopupGravity.Bottom) == PopupGravity.Bottom)
        {
            return new Vector3D(0, -TranslationDistance, 0);
        }

        if ((gravity & PopupGravity.Left) == PopupGravity.Left)
        {
            return new Vector3D(TranslationDistance, 0, 0);
        }

        if ((gravity & PopupGravity.Right) == PopupGravity.Right)
        {
            return new Vector3D(-TranslationDistance, 0, 0);
        }

        return new Vector3D(0, TranslationDistance, 0);
    }
}
