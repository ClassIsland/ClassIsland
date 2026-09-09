using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace ClassIsland.Controls.ScheduleWeekEdit;

internal sealed class ScheduleWeekRuler : Control
{
    public static readonly StyledProperty<double> ScaleProperty =
        AvaloniaProperty.Register<ScheduleWeekRuler, double>(nameof(Scale), 2);

    public double Scale { get => GetValue(ScaleProperty); set => SetValue(ScaleProperty, value); }

    static ScheduleWeekRuler()
    {
        AffectsRender<ScheduleWeekRuler>(TextElement.ForegroundProperty, ScaleProperty);
    }

    public override void Render(DrawingContext context)
    {
        var foreground = GetValue(TextElement.ForegroundProperty) ?? Brushes.Gray;
        var columnWidth = Math.Max(0, Bounds.Width - ScheduleWeekEditControl.RulerWidth) / 7;
        var labelInterval = Scale * 30 >= 18 ? 30 : Scale * 60 >= 18 ? 60 : Scale * 120 >= 18 ? 120 : 240;
        for (var minutes = 0; minutes <= 1440; minutes += 30)
        {
            var y = minutes * Scale;
            using (context.PushOpacity(minutes % 60 == 0 ? 0.35 : 0.14))
                context.DrawLine(new Pen(foreground), new Point(ScheduleWeekEditControl.RulerWidth, y), new Point(Bounds.Width, y));
            if (minutes % labelInterval != 0) continue;
            var label = minutes == 1440 ? "24:00" : TimeSpan.FromMinutes(minutes).ToString(@"hh\:mm");
            var text = new FormattedText(label, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default), 11, foreground);
            using (context.PushOpacity(minutes % 60 == 0 ? 1 : 0.6))
                context.DrawText(text, new Point(3, Math.Clamp(y - 7, 0, Math.Max(0, Bounds.Height - 15))));
        }
        using (context.PushOpacity(0.15))
            for (var day = 0; day <= 7; day++)
            {
                var x = ScheduleWeekEditControl.RulerWidth + day * columnWidth;
                context.DrawLine(new Pen(foreground), new Point(x, 0), new Point(x, Bounds.Height));
            }
    }
}
