using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.Styling;
using ClassIsland.Shared.Interfaces.Controls;

namespace ClassIsland.Controls.NotificationEffects;

/// <summary>
/// RippleEffect.xaml 的交互逻辑
/// </summary>
public partial class RippleEffect : UserControl, INotificationEffectControl
{
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
    

    public RippleEffect()
    {
        InitializeComponent();
    }


    public async void Play()
    { 
        // 计算到达四个顶点的距离，取其最大值作为圆的最大半径。
        var topLevel = TopLevel.GetTopLevel(this);
        var r11 = Math.Sqrt(Math.Pow(CenterX, 2) + Math.Pow(CenterY, 2));
        var r12 = Math.Sqrt(Math.Pow(topLevel?.Width ?? 0 - CenterX, 2) + Math.Pow(CenterY, 2));
        var r21 = Math.Sqrt(Math.Pow(CenterX, 2) + Math.Pow(topLevel?.Height ?? 0  - CenterY, 2));
        var r22 = Math.Sqrt(Math.Pow(topLevel?.Width ?? 0  - CenterX, 2) + Math.Pow(topLevel?.Height ?? 0  - CenterY, 2));
        var r = Math.Ceiling(((List<double>) [r11, r12, r21, r22]).Max());


        var animation = new Animation()
        {
            Duration = TimeSpan.FromSeconds(0.75),
            Children =
            {
                new KeyFrame()
                {
                    Cue = new Cue(0),
                    Setters =
                    {
                        new Setter(OpacityProperty, 1.0),
                        new Setter(EllipseSizeProperty, 0.0)
                    }
                },
                new KeyFrame()
                {
                    Cue = new Cue(1),
                    Setters =
                    {
                        new Setter(OpacityProperty, 0.0),
                        new Setter(EllipseSizeProperty, Math.Ceiling(r * 2))
                    }
                }
            },
            Easing = new CubicEaseOut()
        };
        await animation.RunAsync(this);
        EffectCompleted?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? EffectCompleted;
}