using System.Windows;
using System.Windows.Controls;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 具有 MaterialDesign 外观的 <see cref="ProgressBar"/>。
/// </summary>
public class MaterialProgressBar : ProgressBar
{
    public static readonly DependencyProperty ForegroundScaleProperty = DependencyProperty.Register(
        nameof(ForegroundScale), typeof(double), typeof(MaterialProgressBar), new PropertyMetadata(default(double)));

    public double ForegroundScale
    {
        get { return (double)GetValue(ForegroundScaleProperty); }
        set { SetValue(ForegroundScaleProperty, value); }
    }


}