using Avalonia;
using Avalonia.Animation.Easings;
using SmoothScroll.Avalonia.Controls;

namespace ClassIsland.Core.Assists;

public class ScrollPresenterAnimationAssist
{
    public static readonly AttachedProperty<bool> IsTeleportationScrollEnabledProperty =
        AvaloniaProperty.RegisterAttached<ScrollPresenterAnimationAssist, ScrollPresenter, bool>("IsTeleportationScrollEnabled");

    public static void SetIsTeleportationScrollEnabled(ScrollPresenter obj, bool value) => obj.SetValue(IsTeleportationScrollEnabledProperty, value);
    public static bool GetIsTeleportationScrollEnabled(ScrollPresenter obj) => obj.GetValue(IsTeleportationScrollEnabledProperty);
    
    static ScrollPresenterAnimationAssist()
    {
        IsTeleportationScrollEnabledProperty.Changed.AddClassHandler<ScrollPresenter>(HandleIsTeleportationScrollEnabledChanged);
    }

    private static void HandleIsTeleportationScrollEnabledChanged(ScrollPresenter arg1, AvaloniaPropertyChangedEventArgs arg2)
    {
        arg1.ScrollAnimationStarting -= ScrollPresenterOnScrollAnimationStarting;
        if (GetIsTeleportationScrollEnabled(arg1))
        {
            arg1.ScrollAnimationStarting += ScrollPresenterOnScrollAnimationStarting;
        }
    }

    private static void ScrollPresenterOnScrollAnimationStarting(object? sender, ScrollAnimationStartingEventArgs e)
    {
        // from https://github.com/zxbmmmmmmmmm/SmoothScroll.Avalonia/blob/master/samples/SmoothScroll.Avalonia.Sample/Views/Pages/ListPage.axaml.cs#L39
        var compositor = e.Animation.Compositor;
        var targetVerticalPosition = e.EndPosition.Y;

        var teleportationAnimation = compositor.CreateVector3DKeyFrameAnimation();

        var deltaVerticalPosition = targetVerticalPosition - e.StartingPosition.Y;

        var cubicBezierStart = new CreateCubicBezierEasing(
            new Vector(1.0f, 0.0f), // Control point 1
            new Vector(1.0f, 0.0f)); // Control point 2

        var step = new StepEasing();
        
        var cubicBezierEnd = new CreateCubicBezierEasing(
            new Vector(0.0, 1.0), // Control point 1
            new Vector(0.0, 1.0)); // Control point 2

        teleportationAnimation.InsertKeyFrame(
            0.499999f,
            new Vector3D(e.StartingPosition.X, targetVerticalPosition - 0.9f * deltaVerticalPosition, 0.0f),
            cubicBezierStart); 


        teleportationAnimation.InsertKeyFrame(
            0.5f,
            new Vector3D(e.StartingPosition.X, targetVerticalPosition - 0.1f * deltaVerticalPosition, 0.0f),
            step);
        
        teleportationAnimation.InsertKeyFrame(
            1.0f, 
            new Vector3D(e.EndPosition.X, targetVerticalPosition, 0.0f),
            cubicBezierEnd); 
        
        teleportationAnimation.Duration = TimeSpan.FromMilliseconds(750);
        
        e.Animation = teleportationAnimation;
    }
}

internal class CreateCubicBezierEasing : Easing
{
    private const double SolveEpsilon = 1e-6;
    private const int NewtonIterations = 8;

    private readonly double _ax;
    private readonly double _bx;
    private readonly double _cx;
    private readonly double _ay;
    private readonly double _by;
    private readonly double _cy;

    public CreateCubicBezierEasing(Vector controlPoint1, Vector controlPoint2)
    {
        if (controlPoint1.X is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(controlPoint1), "The X coordinate must be in the [0, 1] range.");

        if (controlPoint2.X is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(controlPoint2), "The X coordinate must be in the [0, 1] range.");

        _cx = 3 * controlPoint1.X;
        _bx = 3 * (controlPoint2.X - controlPoint1.X) - _cx;
        _ax = 1 - _cx - _bx;

        _cy = 3 * controlPoint1.Y;
        _by = 3 * (controlPoint2.Y - controlPoint1.Y) - _cy;
        _ay = 1 - _cy - _by;
    }

    public override double Ease(double progress)
    {
        if (progress <= 0)
            return 0;

        if (progress >= 1)
            return 1;

        var parameter = SolveCurveX(progress);
        return SampleCurveY(parameter);
    }

    private double SampleCurveX(double t)
    {
        return ((_ax * t + _bx) * t + _cx) * t;
    }

    private double SampleCurveY(double t)
    {
        return ((_ay * t + _by) * t + _cy) * t;
    }

    private double SampleCurveDerivativeX(double t)
    {
        return (3 * _ax * t + 2 * _bx) * t + _cx;
    }

    private double SolveCurveX(double x)
    {
        var t = x;
        for (var i = 0; i < NewtonIterations; i++)
        {
            var currentX = SampleCurveX(t) - x;
            if (Math.Abs(currentX) < SolveEpsilon)
                return t;

            var derivative = SampleCurveDerivativeX(t);
            if (Math.Abs(derivative) < SolveEpsilon)
                break;

            t -= currentX / derivative;
        }

        var lower = 0d;
        var upper = 1d;
        t = x;

        while (lower < upper)
        {
            var currentX = SampleCurveX(t);
            if (Math.Abs(currentX - x) < SolveEpsilon)
                return t;

            if (x > currentX)
                lower = t;
            else
                upper = t;

            var next = (lower + upper) / 2;
            if (Math.Abs(next - t) < SolveEpsilon)
                return next;

            t = next;
        }

        return t;
    }
}

internal class StepEasing : Easing
{
    public override double Ease(double progress)
    {
        return progress < 0.5 ? 0 : 1;
    }
}