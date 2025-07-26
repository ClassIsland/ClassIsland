using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using ReactiveUI;

namespace ClassIsland.Core.Behaviors;

/// <summary>
/// <see cref="StackPanel"/> 进入动画行为。
/// </summary>
public class StackPanelIntroAnimationBehavior
{
    public static readonly AttachedProperty<bool> IsIntroAnimationEnabledProperty =
        AvaloniaProperty.RegisterAttached<StackPanelIntroAnimationBehavior, Panel, bool>("IsIntroAnimationEnabled");

    public static void SetIsIntroAnimationEnabled(Panel obj, bool value) => obj.SetValue(IsIntroAnimationEnabledProperty, value);
    public static bool GetIsIntroAnimationEnabled(Panel obj) => obj.GetValue(IsIntroAnimationEnabledProperty);

    public static readonly AttachedProperty<bool> IsAnimationPlayedProperty =
        AvaloniaProperty.RegisterAttached<StackPanelIntroAnimationBehavior, Control, bool>("IsAnimationPlayed");

    public static void SetIsAnimationPlayed(Control obj, bool value) => obj.SetValue(IsAnimationPlayedProperty, value);
    public static bool GetIsAnimationPlayed(Control obj) => obj.GetValue(IsAnimationPlayedProperty);

    public static readonly AttachedProperty<bool> CanPlayAnimationProperty =
        AvaloniaProperty.RegisterAttached<StackPanelIntroAnimationBehavior, Control, bool>("CanPlayAnimation");

    public static void SetCanPlayAnimation(Control obj, bool value) => obj.SetValue(CanPlayAnimationProperty, value);
    public static bool GetCanPlayAnimation(Control obj) => obj.GetValue(CanPlayAnimationProperty);

    public static readonly AttachedProperty<bool> IsAnimationPlayingStartedProperty =
        AvaloniaProperty.RegisterAttached<StackPanelIntroAnimationBehavior, Panel, bool>("IsAnimationPlayingStarted");

    public static void SetIsAnimationPlayingStarted(Panel obj, bool value) => obj.SetValue(IsAnimationPlayingStartedProperty, value);
    public static bool GetIsAnimationPlayingStarted(Panel obj) => obj.GetValue(IsAnimationPlayingStartedProperty);

    static StackPanelIntroAnimationBehavior()
    {
        IsIntroAnimationEnabledProperty.Changed.AddClassHandler<Panel>(HandleIsIntroAnimationChanged);
    }

    private static void HandleIsIntroAnimationChanged(Panel panel, AvaloniaPropertyChangedEventArgs args)
    {
        if (!GetIsIntroAnimationEnabled(panel) || GetIsAnimationPlayingStarted(panel) || IThemeService.AnimationLevel <= 1)
        {
            return;
        }

        var animable = panel.Children
            .Where(x => x.RenderTransform == null && x.IsVisible)
            .ToList();
        foreach (var c in animable)
        {
            SetupAnimation(c);
        }

        panel.Loaded += (_, _) =>
        {
            StartAnimation(panel);
        };
    }


    private static void SetupAnimation(Control control)
    {
        SetCanPlayAnimation(control, true);
        var visual = ElementComposition.GetElementVisual(control);
        if (visual == null)
        {
            return;
        }

        visual.Opacity = 0f;
    }
    
    private static void BeginAnimation(Control control)
    {
        var visual = ElementComposition.GetElementVisual(control);
        if (visual == null)
        {
            return;
        }

        var compositor = visual.Compositor;
        var group = compositor.CreateAnimationGroup();
        var animOffset = compositor.CreateVector3DKeyFrameAnimation();
        animOffset.Target = nameof(visual.Offset);
        animOffset.Duration = TimeSpan.FromMilliseconds(750);
        var offsetRaw = visual.Offset.Y;
        // Console.WriteLine($"{offsetRaw}");
        animOffset.InsertKeyFrame(0f, visual.Offset with {  Y = offsetRaw + 50 });
        animOffset.InsertKeyFrame(1f, visual.Offset with {  Y = offsetRaw }, Easing.Parse("0.00, 1.00, 0.00, 1.00"));
        animOffset.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
        group.Add(animOffset);
        var animOpacity = compositor.CreateScalarKeyFrameAnimation();
        animOpacity.Target = nameof(visual.Opacity);
        animOpacity.Duration = TimeSpan.FromMilliseconds(400);
        animOpacity.InsertKeyFrame(0f, 0f);
        animOpacity.InsertKeyFrame(1f, 1f, Easing.Parse("0.00, 1.00, 0.00, 1.00"));
        animOpacity.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
        group.Add(animOpacity);
        visual.StartAnimationGroup(group);
    }

    private static void StartAnimation(Panel panel)
    {
        if (!GetIsIntroAnimationEnabled(panel) || GetIsAnimationPlayingStarted(panel))
        {
            return;
        }

        var targets = panel.Children.Where(GetCanPlayAnimation).ToList();
        var index = 0;
        var timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromSeconds(0.025)
        };
        timer.Tick += (sender, args) =>
        {
            if (index >= targets.Count)
            {
                timer.Stop();
                SetIsAnimationPlayingStarted(panel, false);
                return;
            }

            BeginAnimation(targets[index]);
            index++;
        };
        SetIsAnimationPlayingStarted(panel, true);
        timer.Start();
    }

}