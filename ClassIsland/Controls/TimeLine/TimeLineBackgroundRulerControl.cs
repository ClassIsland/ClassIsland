using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace ClassIsland.Controls.TimeLine;

public class TimeLineBackgroundRulerControl : Control
{
    public static readonly StyledProperty<double> ScaleProperty = AvaloniaProperty.Register<TimeLineBackgroundRulerControl, double>(
        nameof(Scale), 1.0);

    public double Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }
    private static double BaseTicks { get; } = 1000000000.0;

    private static TimeSpan BaseSpan { get; } = TimeSpan.FromMinutes(5);


    static TimeLineBackgroundRulerControl()
    {
        
    }

    public override void Render(DrawingContext drawingContext)
    {
        var ts = TimeSpan.Zero;
        var lastY = 0.0;
        var c = 0;
        var p = 12;
        var bs = TimeSpan.FromHours(24).Ticks / BaseTicks * Scale / 36;
        var body = GetValue(TextElement.ForegroundProperty) as SolidColorBrush ?? new SolidColorBrush();
        var bodyA = new SolidColorBrush(body.Color)
        {
            Opacity = 0.3
        };
        var pen = new Pen(body, 1);
        var penA = new Pen(bodyA, 1);
        var fontFamily = this.FindResource("AppFont") as FontFamily ?? FontFamily.Default;
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
            if ((c * 2) % p != 0)
            {
                goto done;
            }

            drawingContext.DrawLine(c % p == 0 ? pen : penA, new Point(36, y), new Point(Bounds.Width, y));
            if (c % p != 0)
            {
                goto done;
            }

            var text = new FormattedText(ts.ToString("hh\\:mm"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(fontFamily), 12, body);
            drawingContext.DrawText(text, new Point(0, y - 8));

            done:
            ts += BaseSpan;
            c++;
        } while (ts < TimeSpan.FromHours(24));

        base.Render(drawingContext);
    }
}