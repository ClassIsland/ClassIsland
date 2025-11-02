using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using ClassIsland.Shared.Interfaces.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClassIsland.Controls.NotificationEffects;

/// <summary>
/// RippleEffect.xaml 的交互逻辑
/// </summary>
public partial class RippleEffect : UserControl, INotificationEffectControl
{
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<RippleEffect, IBrush?>(
            nameof(Fill));

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public static readonly StyledProperty<double> CenterXProperty = AvaloniaProperty.Register<RippleEffect, double>(
        nameof(CenterX));

    public double CenterX
    {
        get => GetValue(CenterXProperty);
        set => SetValue(CenterXProperty, value);
    }

    public static readonly StyledProperty<double> CenterYProperty = AvaloniaProperty.Register<RippleEffect, double>(
        nameof(CenterY));

    public double CenterY
    {
        get => GetValue(CenterYProperty);
        set => SetValue(CenterYProperty, value);
    }

    public static readonly StyledProperty<double> EllipseSizeProperty = AvaloniaProperty.Register<RippleEffect, double>(
        nameof(EllipseSize));

    public double EllipseSize
    {
        get => GetValue(EllipseSizeProperty);
        set => SetValue(EllipseSizeProperty, value);
    }
    
    private PixelPoint CenterPoint { get; }

    public RippleEffect(PixelPoint center, IBrush? brush = null)
    {
        CenterPoint = center;
        Fill = brush ?? (Application.Current!.TryFindResource("AccentFillColorDefaultBrush", out var v) ? (IBrush)v : null);
        InitializeComponent();
        EllipseMain.IsVisible = true;
    }


    public async void Play()
    { 
        // 计算到达四个顶点的距离，取其最大值作为圆的最大半径。
        var (cx, cy) = this.PointToClient(CenterPoint);
        CenterX = cx;
        CenterY = cy;
        var topLevel = TopLevel.GetTopLevel(this);
        var r11 = Math.Sqrt(Math.Pow(cx, 2) + Math.Pow(cy, 2));
        var r12 = Math.Sqrt(Math.Pow(topLevel?.Width ?? 0 - cx, 2) + Math.Pow(cy, 2));
        var r21 = Math.Sqrt(Math.Pow(cx, 2) + Math.Pow(topLevel?.Height ?? 0  - cy, 2));
        var r22 = Math.Sqrt(Math.Pow(topLevel?.Width ?? 0  - cx, 2) + Math.Pow(topLevel?.Height ?? 0  - cy, 2));
        var r = Math.Ceiling(((List<double>) [r11, r12, r21, r22]).Max());

        EllipseSize = EllipseMain.Width = EllipseMain.Height = r * 2;
        var visual = ElementComposition.GetElementVisual(EllipseMain);
        if (visual == null)
        {
            return;
        }
        visual.Scale = new Vector3D(0, 0, 0);
        visual.Opacity = 1.0f;
        // visual.CenterPoint = visual.CenterPoint with {X = r, Y = r};
        // visual.Offset = visual.Offset with{X = -1000, Y = r};
        // visual.AnchorPoint = new Vector(cx, cy);
        var compositor = visual.Compositor;
        var animationScale = compositor.CreateVector3DKeyFrameAnimation();
        animationScale.InsertKeyFrame(0f, new Vector3D(0, 0, 0));
        animationScale.InsertKeyFrame(1f, new Vector3D(1, 1, 1), new QuadraticEaseIn());
        animationScale.Duration = TimeSpan.FromMilliseconds(600);
        visual.StartAnimation(nameof(visual.Scale), animationScale);
        // var animationOffset = compositor.CreateVector3DKeyFrameAnimation();
        // animationOffset.InsertKeyFrame(0f, visual.Offset with { X = 0, Y = 0});
        // animationOffset.InsertKeyFrame(1f, visual.Offset with { X = -r * 2, Y = -r * 2});
        // animationOffset.Duration = TimeSpan.FromMilliseconds(750);
        // visual.StartAnimation(nameof(visual.Offset), animationOffset);
        var animationOpacity = compositor.CreateScalarKeyFrameAnimation();
        animationOpacity.InsertKeyFrame(0f, 1f);
        animationOpacity.InsertKeyFrame(1f, 0f, new SineEaseIn());
        animationOpacity.Duration = TimeSpan.FromMilliseconds(600);
        visual.StartAnimation(nameof(visual.Opacity), animationOpacity);
        
        await Task.Delay(750);
        EffectCompleted?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? EffectCompleted;
}