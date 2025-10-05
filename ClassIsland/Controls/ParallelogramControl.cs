using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ClassIsland.Controls;

public class ParallelogramControl : Control
{
    // 平行四边形的垂直高度（用户指定）
    public static readonly StyledProperty<double> SlantedHeightProperty =
        AvaloniaProperty.Register<ParallelogramControl, double>(
            nameof(SlantedHeight), 40.0);

    // 填充
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<ParallelogramControl, IBrush?>(
            nameof(Fill), Brushes.LightSkyBlue);

    // 描边
    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<ParallelogramControl, IBrush?>(
            nameof(Stroke), Brushes.SteelBlue);

    // 描边宽度
    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<ParallelogramControl, double>(
            nameof(StrokeThickness), 2.0);

    public double SlantedHeight
    {
        get => GetValue(SlantedHeightProperty);
        set => SetValue(SlantedHeightProperty, value);
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    // 当可用空间测量时，返回希望占用的高度（以 SlantedHeight 为主）
    protected override Size MeasureOverride(Size availableSize)
    {
        // 如果父容器没有给定宽度（Infinity），给个默认宽度
        double width = double.IsInfinity(availableSize.Width) ? 100 : availableSize.Width;
        double height = SlantedHeight;
        return new Size(width, height);
    }

    // 绘制
    public override void Render(DrawingContext context)
    {
        var size = Bounds.Size;
        var h = SlantedHeight;

        if (h < 0) h = 0;
        if (size.Height > 0 && size.Height < h)
            h = size.Height;

        var offset = Math.Tan(Math.PI / 6) * h;

        var w = size.Width;
        if (w <= 0 || double.IsInfinity(w) || double.IsNaN(w))
            w = Math.Max(120, offset + 10);

        if (offset * 2 > w)
            offset = w / 2.0;

        // 四个顶点（顺时针或逆时针都可以）
        // top-left, top-right, bottom-right, bottom-left
        var p1 = new Point(offset, 0); // top-left
        var p2 = new Point(w, 0); // top-right
        var p3 = new Point(w - offset, h); // bottom-right
        var p4 = new Point(0, h); // bottom-left

        // 创建几何并绘制
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(p1, isFilled: true);
            ctx.LineTo(p2);
            ctx.LineTo(p3);
            ctx.LineTo(p4);
            ctx.LineTo(p1);
            ctx.EndFigure(isClosed: true);
        }

        var pen = Stroke != null && StrokeThickness > 0
            ? new Pen(Stroke, StrokeThickness)
            : null;

        context.DrawGeometry(Fill, pen, geometry);
    }
    
    
}