using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace ClassIsland.Controls;

/// <summary>
/// 将提醒背景材质和内容裁剪到带连接尖角的气泡轮廓。
/// </summary>
public class RectanglesNotificationBubble : Panel
{
    public const double BodyHeight = 40;
    public const double TipLength = 10;
    private const double ContentPadding = 12;

    public static readonly StyledProperty<double> BodyWidthProperty =
        AvaloniaProperty.Register<RectanglesNotificationBubble, double>(nameof(BodyWidth), 325);

    public static readonly StyledProperty<int> WindowDockingLocationProperty =
        AvaloniaProperty.Register<RectanglesNotificationBubble, int>(nameof(WindowDockingLocation), 1);

    public static readonly StyledProperty<double> CornerRadiusProperty =
        AvaloniaProperty.Register<RectanglesNotificationBubble, double>(nameof(CornerRadius), 8);

    public static FuncValueConverter<double, double> InverseProgressConverter { get; } =
        new(value => 1 - Math.Clamp(value, 0, 1));

    private Rect _outlineBody;
    private int _outlineDock;
    private double _outlineRadius;

    public double BodyWidth
    {
        get => GetValue(BodyWidthProperty);
        set => SetValue(BodyWidthProperty, value);
    }

    public int WindowDockingLocation
    {
        get => GetValue(WindowDockingLocationProperty);
        set => SetValue(WindowDockingLocationProperty, value);
    }

    public double CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    static RectanglesNotificationBubble()
    {
        AffectsMeasure<RectanglesNotificationBubble>(BodyWidthProperty, WindowDockingLocationProperty);
        AffectsArrange<RectanglesNotificationBubble>(CornerRadiusProperty);

        // A clipped descendant can keep the same DesiredSize after its natural content width changes.
        foreach (var property in new AvaloniaProperty[]
                 {
                     TextBlock.TextProperty, ContentPresenter.ContentProperty, TextBlock.InlinesProperty,
                     TextElement.FontFamilyProperty, TextElement.FontSizeProperty, TextElement.FontWeightProperty,
                     TextElement.FontStyleProperty, TextElement.LetterSpacingProperty,
                     WidthProperty, MinWidthProperty, MaxWidthProperty, MarginProperty, IsVisibleProperty
                 })
        {
            property.Changed.AddClassHandler<Control>((control, _) => InvalidateContentMeasure(control));
        }
        Run.TextProperty.Changed.AddClassHandler<Run>((run, _) =>
            InvalidateContentMeasure(run.FindLogicalAncestorOfType<TextBlock>()));
    }

    internal double MeasureContentWidth(bool overlay)
    {
        if (Children.Count != 4)
        {
            return 0;
        }

        var content = Children[overlay ? 3 : 2];
        var padding = overlay ? 0 : ContentPadding * 2;
        // Star columns and trimmed text must report their natural width before the bubble applies its cap.
        content.Measure(new Size(double.PositiveInfinity, BodyHeight));
        return GetPreferredContentWidth(content) + padding;
    }

    private static double GetPreferredContentWidth(Control control)
    {
        if (!control.IsVisible || double.IsFinite(control.Width))
        {
            return control.DesiredSize.Width;
        }

        var children = control.GetVisualChildren().OfType<Control>()
            .Where(child => child.IsVisible)
            .Select(child => (Control: child, Width: GetPreferredContentWidth(child)))
            .ToArray();
        var extraWidths = children.Select(child => Math.Max(0, child.Width - child.Control.DesiredSize.Width));
        var extraWidth = control is StackPanel { Orientation: Orientation.Horizontal }
            ? extraWidths.Sum()
            : extraWidths.DefaultIfEmpty(0).Max();
        var width = control.DesiredSize.Width + extraWidth;

        if (control is Grid { ColumnDefinitions.Count: > 0 } grid &&
            children.All(child => Grid.GetColumnSpan(child.Control) == 1))
        {
            var columns = grid.ColumnDefinitions;
            var columnWidths = columns.Select(column => Math.Clamp(
                column.Width.IsAbsolute ? column.Width.Value : column.MinWidth,
                column.MinWidth, column.MaxWidth)).ToArray();
            foreach (var child in children)
            {
                var index = Math.Min(Grid.GetColumn(child.Control), columns.Count - 1);
                var column = columns[index];
                if (!column.Width.IsAbsolute)
                {
                    columnWidths[index] = Math.Clamp(Math.Max(columnWidths[index], Math.Ceiling(child.Width)),
                        column.MinWidth, column.MaxWidth);
                }
            }

            // Infinite measurement treats star columns as auto; finite layout must still honor their ratios.
            var starUnit = columns.Select((column, index) => column.Width.IsStar && column.Width.Value > 0
                ? columnWidths[index] / column.Width.Value
                : 0).DefaultIfEmpty(0).Max();
            var requiredWidth = columns.Select((column, index) => column.Width.IsStar
                ? Math.Clamp(starUnit * column.Width.Value, column.MinWidth, column.MaxWidth)
                : columnWidths[index]).Sum();
            requiredWidth += grid.ColumnSpacing * (columns.Count - 1) + grid.Margin.Left + grid.Margin.Right;
            width = Math.Max(control.DesiredSize.Width, requiredWidth);
        }

        return Math.Min(width, control.MaxWidth + control.Margin.Left + control.Margin.Right);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var body = GetBodyRect();
        var size = WindowDockingLocation is 1 or 4
            ? new Size(BodyWidth, BodyHeight + TipLength)
            : new Size(BodyWidth + TipLength, BodyHeight);
        for (var i = 0; i < Children.Count; i++)
        {
            Children[i].Measure(i < 2 ? size : i == 3
                ? body.Size
                : new Size(Math.Max(0, body.Width - ContentPadding * 2), body.Height));
        }

        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var body = GetBodyRect();
        if (Clip == null || body != _outlineBody || WindowDockingLocation != _outlineDock || CornerRadius != _outlineRadius)
        {
            Clip = CreateOutline(body);
            _outlineBody = body;
            _outlineDock = WindowDockingLocation;
            _outlineRadius = CornerRadius;
        }
        var content = new Rect(body.X + Math.Min(ContentPadding, body.Width / 2), body.Y,
            Math.Max(0, body.Width - ContentPadding * 2), body.Height);
        for (var i = 0; i < Children.Count; i++)
        {
            Children[i].Arrange(i < 2 ? new Rect(finalSize) : i == 3 ? body : content);
        }

        return finalSize;
    }

    private static void InvalidateContentMeasure(Control? control)
    {
        var host = control?.FindAncestorOfType<RectanglesNotificationBubble>()
            ?.FindAncestorOfType<RectanglesNotificationPanel>();
        if (host == null)
        {
            return;
        }

        // Re-measuring only the host would still reuse cached measurements of the content wrappers.
        for (Visual? ancestor = control; ancestor != null; ancestor = ancestor.GetVisualParent())
        {
            if (ancestor is Layoutable layoutable)
            {
                layoutable.InvalidateMeasure();
            }
            if (ancestor == host)
            {
                break;
            }
        }
    }

    private Rect GetBodyRect()
    {
        return WindowDockingLocation switch
        {
            1 => new Rect(0, TipLength, BodyWidth, BodyHeight),
            0 or 3 => new Rect(TipLength, 0, BodyWidth, BodyHeight),
            _ => new Rect(0, 0, BodyWidth, BodyHeight)
        };
    }

    private Geometry CreateOutline(Rect body)
    {
        var radius = Math.Clamp(CornerRadius, 0, Math.Min(body.Width, body.Height) / 2);
        var rectangle = new RectangleGeometry(body, radius, radius);
        var triangle = new StreamGeometry();
        var center = body.Center;
        var halfBase = Math.Min(TipLength, BodyWidth / 2);
        Point tip;
        Point start;
        Point end;
        switch (WindowDockingLocation)
        {
            case 1:
                tip = new Point(center.X, 0);
                start = new Point(center.X - halfBase, body.Top);
                end = new Point(center.X + halfBase, body.Top);
                break;
            case 4:
                tip = new Point(center.X, BodyHeight + TipLength);
                start = new Point(center.X - halfBase, body.Bottom);
                end = new Point(center.X + halfBase, body.Bottom);
                break;
            case 0 or 3:
                tip = new Point(0, center.Y);
                start = new Point(body.Left, center.Y - TipLength);
                end = new Point(body.Left, center.Y + TipLength);
                break;
            default:
                tip = new Point(BodyWidth + TipLength, center.Y);
                start = new Point(body.Right, center.Y - TipLength);
                end = new Point(body.Right, center.Y + TipLength);
                break;
        }

        using (var context = triangle.Open())
        {
            context.BeginFigure(start, true);
            context.LineTo(tip);
            context.LineTo(end);
            context.EndFigure(true);
        }

        return new CombinedGeometry(GeometryCombineMode.Union, rectangle, triangle);
    }
}
