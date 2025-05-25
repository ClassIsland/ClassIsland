using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

using ClassIsland.Shared.Interfaces.Controls;

namespace ClassIsland.Controls.NotificationEffects;

/// <summary>
/// RippleEffect.xaml 的交互逻辑
/// </summary>
public partial class RippleEffect : UserControl, INotificationEffectControl
{
    public static readonly DependencyProperty CenterXProperty = DependencyProperty.Register(
        nameof(CenterX), typeof(double), typeof(RippleEffect), new PropertyMetadata(default(double)));

    public double CenterX
    {
        get { return (double)GetValue(CenterXProperty); }
        set { SetValue(CenterXProperty, value); }
    }

    public static readonly DependencyProperty CenterYProperty = DependencyProperty.Register(
        nameof(CenterY), typeof(double), typeof(RippleEffect), new PropertyMetadata(default(double)));

    public double CenterY
    {
        get { return (double)GetValue(CenterYProperty); }
        set { SetValue(CenterYProperty, value); }
    }

    public static readonly DependencyProperty EllipseSizeProperty = DependencyProperty.Register(
        nameof(EllipseSize), typeof(double), typeof(RippleEffect), new PropertyMetadata(default(double)));

    public double EllipseSize
    {
        get { return (double)GetValue(EllipseSizeProperty); }
        set { SetValue(EllipseSizeProperty, value); }
    }


    public RippleEffect()
    {
        InitializeComponent();
    }


    public void Play()
    { 
        // 计算到达四个顶点的距离，取其最大值作为圆的最大半径。
        var r11 = Math.Sqrt(Math.Pow(CenterX, 2) + Math.Pow(CenterY, 2));
        var r12 = Math.Sqrt(Math.Pow(ActualWidth - CenterX, 2) + Math.Pow(CenterY, 2));
        var r21 = Math.Sqrt(Math.Pow(CenterX, 2) + Math.Pow(ActualHeight - CenterY, 2));
        var r22 = Math.Sqrt(Math.Pow(ActualWidth - CenterX, 2) + Math.Pow(ActualHeight - CenterY, 2));
        var r = Math.Ceiling(((List<double>) [r11, r12, r21, r22]).Max());

        var storyboard = new Storyboard();
        var growing = new DoubleAnimation()
        {
            From = 0,
            To = Math.Ceiling(r * 2), 
            Duration = new Duration(TimeSpan.FromSeconds(0.75)),
            EasingFunction = new CubicEase()
        };
        Storyboard.SetTarget(growing, this);
        Storyboard.SetTargetProperty(growing, new PropertyPath(EllipseSizeProperty));
        storyboard.Children.Add(growing);
        var opacity = new DoubleAnimation()
        {
            From = 1.0,
            To = 0.0,
            Duration = new Duration(TimeSpan.FromSeconds(0.75)),
            EasingFunction = new ExponentialEase()
        };
        Storyboard.SetTarget(opacity, EllipseMain);
        Storyboard.SetTargetProperty(opacity, new PropertyPath(OpacityProperty));
        storyboard.Children.Add(opacity);
        
        storyboard.Completed += (sender, args) => 
            EffectCompleted?.Invoke(this, EventArgs.Empty);
        storyboard.Begin();
    }

    public event EventHandler? EffectCompleted;
}