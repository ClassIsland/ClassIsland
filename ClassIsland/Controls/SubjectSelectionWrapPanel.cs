using System;
using Avalonia;
using Avalonia.Controls;
using ClassIsland.Models.Profile;

namespace ClassIsland.Controls;

/// <summary>
/// 科目选择器的换行面板。分行模式下，每个科目分组都会从新的一行开始。
/// </summary>
public class SubjectSelectionWrapPanel : Panel
{
    public static readonly StyledProperty<bool> IsGroupRowModeProperty =
        AvaloniaProperty.Register<SubjectSelectionWrapPanel, bool>(nameof(IsGroupRowMode));

    public bool IsGroupRowMode
    {
        get => GetValue(IsGroupRowModeProperty);
        set => SetValue(IsGroupRowModeProperty, value);
    }

    static SubjectSelectionWrapPanel()
    {
        AffectsMeasure<SubjectSelectionWrapPanel>(IsGroupRowModeProperty);
        AffectsArrange<SubjectSelectionWrapPanel>(IsGroupRowModeProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var lineWidth = 0.0;
        var lineHeight = 0.0;
        var desiredWidth = 0.0;
        var desiredHeight = 0.0;

        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var childSize = child.DesiredSize;
            if (ShouldStartNewLine(child, lineWidth, childSize.Width, availableSize.Width))
            {
                desiredWidth = Math.Max(desiredWidth, lineWidth);
                desiredHeight += lineHeight;
                lineWidth = 0;
                lineHeight = 0;
            }

            lineWidth += childSize.Width;
            lineHeight = Math.Max(lineHeight, childSize.Height);
        }

        desiredWidth = Math.Max(desiredWidth, lineWidth);
        desiredHeight += lineHeight;
        return new Size(desiredWidth, desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var x = 0.0;
        var y = 0.0;
        var lineHeight = 0.0;

        foreach (var child in Children)
        {
            var childSize = child.DesiredSize;
            if (ShouldStartNewLine(child, x, childSize.Width, finalSize.Width))
            {
                x = 0;
                y += lineHeight;
                lineHeight = 0;
            }

            child.Arrange(new Rect(x, y, childSize.Width, childSize.Height));
            x += childSize.Width;
            lineHeight = Math.Max(lineHeight, childSize.Height);
        }

        return finalSize;
    }

    private bool ShouldStartNewLine(Control child, double lineWidth, double childWidth, double availableWidth)
    {
        if (lineWidth <= 0)
        {
            return false;
        }

        var isGroupHeader = child.DataContext is SubjectSelectionItem { IsGroupHeader: true };
        var exceedsAvailableWidth = !double.IsPositiveInfinity(availableWidth)
                                    && lineWidth + childWidth > availableWidth;
        return IsGroupRowMode && isGroupHeader || exceedsAvailableWidth;
    }
}
