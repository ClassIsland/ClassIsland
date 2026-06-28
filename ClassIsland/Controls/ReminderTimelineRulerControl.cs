using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace ClassIsland.Controls;

/// <summary>
/// 日程时间线背景标尺，绘制24小时刻度线和时间标签。
/// </summary>
public class ReminderTimelineRulerControl : Control
{
    public static readonly StyledProperty<double> ScaleProperty =
        AvaloniaProperty.Register<ReminderTimelineRulerControl, double>(nameof(Scale), 3.0);

    public double Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    private static readonly double BaseTicks = 1000000000.0;

    static ReminderTimelineRulerControl()
    {
        AffectsRender<ReminderTimelineRulerControl>(ScaleProperty);
    }

    public override void Render(DrawingContext drawingContext)
    {
        var width = Bounds.Width;
        var fore = (GetValue(TextElement.ForegroundProperty) as SolidColorBrush) ?? new SolidColorBrush(Colors.Gray);
        var minorColor = new SolidColorBrush(fore.Color) { Opacity = 0.15 };
        var majorColor = new SolidColorBrush(fore.Color) { Opacity = 0.3 };

        var minorPen = new Pen(minorColor, 1);
        var majorPen = new Pen(majorColor, 1);

        var fontFamily = this.FindResource("AppFont") as FontFamily ?? FontFamily.Default;

        // 确定显示间隔
        var interval = TimeSpan.FromMinutes(5);
        var hourInterval = TimeSpan.FromHours(1);

        var ts = TimeSpan.Zero;
        while (ts < TimeSpan.FromHours(24))
        {
            var y = ts.Ticks / BaseTicks * Scale;

            if (ts.Minutes == 0)
            {
                // 整点线 + 标签
                drawingContext.DrawLine(majorPen, new Point(40, y), new Point(width, y));

                var text = new FormattedText(
                    ts.ToString("hh\\:mm"),
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(fontFamily),
                    11,
                    fore);
                drawingContext.DrawText(text, new Point(2, y - 7));
            }
            else if (ts.Minutes == 30)
            {
                // 半点线（虚线效果：短线段）
                drawingContext.DrawLine(minorPen, new Point(44, y), new Point(width, y));
            }

            ts += interval;
        }

        base.Render(drawingContext);
    }
}
