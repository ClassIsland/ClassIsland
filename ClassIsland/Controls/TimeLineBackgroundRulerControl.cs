using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClassIsland.Controls;

/// <summary>
/// 按照步骤 1a 或 1b 操作，然后执行步骤 2 以在 XAML 文件中使用此自定义控件。
///
/// 步骤 1a) 在当前项目中存在的 XAML 文件中使用该自定义控件。
/// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根
/// 元素中:
///
///     xmlns:MyNamespace="clr-namespace:ClassIsland.Controls"
///
///
/// 步骤 1b) 在其他项目中存在的 XAML 文件中使用该自定义控件。
/// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根
/// 元素中:
///
///     xmlns:MyNamespace="clr-namespace:ClassIsland.Controls;assembly=ClassIsland.Controls"
///
/// 您还需要添加一个从 XAML 文件所在的项目到此项目的项目引用，
/// 并重新生成以避免编译错误:
///
///     在解决方案资源管理器中右击目标项目，然后依次单击
///     “添加引用”->“项目”->[浏览查找并选择此项目]
///
///
/// 步骤 2)
/// 继续操作并在 XAML 文件中使用控件。
///
///     <MyNamespace:TimeLineBackgroundRulerControl/>
///
/// </summary>
public class TimeLineBackgroundRulerControl : Control
{
    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
        nameof(Scale), typeof(double), typeof(TimeLineBackgroundRulerControl), new PropertyMetadata(1.0));

    public double Scale
    {
        get { return (double)GetValue(ScaleProperty); }
        set { SetValue(ScaleProperty, value); }
    }
    private static double BaseTicks { get; } = 1000000000.0;

    private static TimeSpan BaseSpan { get; } = TimeSpan.FromMinutes(5);


    static TimeLineBackgroundRulerControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeLineBackgroundRulerControl), new FrameworkPropertyMetadata(typeof(TimeLineBackgroundRulerControl)));
    }

    protected override void OnRender(DrawingContext drawingContext)
    {

        var ts = TimeSpan.Zero;
        var lastY = 0.0;
        var c = 0;
        var p = 12;
        var bs = TimeSpan.FromHours(24).Ticks / BaseTicks * Scale / 36;
        var body = (SolidColorBrush)FindResource("MaterialDesignBody");
        var bodyA = new SolidColorBrush(body.Color)
        {
            Opacity = 0.3
        };
        var pen = new Pen(body, 1);
        var penA = new Pen(bodyA, 1);
        var fontFamily = (FontFamily)TryFindResource("HarmonyOsSans");
        p = bs switch
        {
            <= 36 => 12,
            > 36 and <= 72 => 6,
            > 72 => 2,
            _ => p
        };
        do
        {
            var y = ts.Ticks / BaseTicks * Scale;
            var yDelta = y - lastY;
            lastY = y;
            if (c * 2 % p != 0)
            {
                goto done;
            }

            drawingContext.DrawLine(c % p == 0 ? pen : penA, new Point(55, y), new Point(ActualWidth, y));
            if (c % p != 0)
            {
                goto done;
            }

            var text = new FormattedText(ts.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(fontFamily, new FontStyle(), FontWeights.Regular,
                    new FontStretch()), 12, body);
            drawingContext.DrawText(text, new Point(0, y - 8));

            done:
            ts += BaseSpan;
            c++;
        } while (ts < TimeSpan.FromHours(24));

        base.OnRender(drawingContext);
    }
}