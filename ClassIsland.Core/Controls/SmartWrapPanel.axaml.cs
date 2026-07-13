using Avalonia;
using Avalonia.Controls;
using ClassIsland.Core.Attributes;
using static Avalonia.AvaloniaProperty;
namespace ClassIsland.Core.Controls;

/// <summary>
/// 智能布局控件。由 DeepSeek 编写。
/// </summary>
public partial class SmartWrapPanel : UserControl
{
    public SmartWrapPanel() => InitializeComponent();

    public static readonly StyledProperty<object> LeftProperty = Register<SmartWrapPanel, object>(nameof(Left));

    public object Left
    {
        get => GetValue(LeftProperty);
        set => SetValue(LeftProperty, value);
    }

    public static readonly StyledProperty<object> MainProperty = Register<SmartWrapPanel, object>(nameof(Main));

    public object Main
    {
        get => GetValue(MainProperty);
        set => SetValue(MainProperty, value);
    }

    public static readonly StyledProperty<ContributorInfo> ContributorInfoProperty = Register<SmartWrapPanel, ContributorInfo>(nameof(ContributorInfo));

    public ContributorInfo ContributorInfo
    {
        get => GetValue(ContributorInfoProperty);
        set => SetValue(ContributorInfoProperty, value);
    }

    public static readonly StyledProperty<object> RightProperty = Register<SmartWrapPanel, object>(nameof(Right));

    public object Right
    {
        get => GetValue(RightProperty);
        set => SetValue(RightProperty, value);
    }
}

class SmartWrapLayout : Panel
{
    enum Strategy { LMBR, LRB_M }

    readonly Placement[] _placements = new Placement[4];

    readonly record struct Placement(double X, double Y, double W, double H);

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var availWidth = double.IsFinite(availableSize.Width) ? availableSize.Width : double.PositiveInfinity;

        foreach (var control in Children)
            control.Measure(new(availWidth, double.PositiveInfinity));

        Size left = Children[0].DesiredSize,
             main = Children[1].DesiredSize,
            badge = Children[2].DesiredSize,
            right = Children[3].DesiredSize;

        // 总宽 → 决定策略
        double totalWidth = left.Width + main.Width + badge.Width + right.Width, totalHeight;
        
        switch (totalWidth <= availWidth ? Strategy.LMBR : Strategy.LRB_M)
        {
            case Strategy.LMBR:
            {
                // [L][..M..][B][R]
                totalHeight = Max(left.Height, main.Height, badge.Height, right.Height);

                _placements[0] = new(0, 0, left.Width,  totalHeight);
                _placements[1] = new(left.Width, 0, availWidth - left.Width - badge.Width - right.Width,  totalHeight);
                _placements[2] = new(availWidth - right.Width - badge.Width, 0, badge.Width, totalHeight);
                _placements[3] = new(availWidth - right.Width, 0, right.Width, totalHeight);

                break;
            }
            case Strategy.LRB_M:
            {
                // [L]..[B][R]
                // [....M....]
                var firstRowHeight = Max(left.Height, right.Height, badge.Height);

                _placements[0] = new(0, 0, left.Width,  firstRowHeight);
                _placements[1] = new(0, firstRowHeight, availWidth, main.Height);
                _placements[2] = new(availWidth - right.Width - badge.Width, 0, badge.Width, firstRowHeight);
                _placements[3] = new(availWidth - right.Width, 0, right.Width, firstRowHeight);

                totalHeight = firstRowHeight + main.Height;
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        return new(availWidth, totalHeight);

        static double Max(double a, double b, double c, double d = 0)
        {
            var m = a;
            if (b > m) m = b;
            if (c > m) m = c;
            if (d > m) m = d;
            return m;
        }
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        for (var i = 0; i < Children.Count; i++)
        {
            var (x, y, w, h) = _placements[i];
            Children[i].Arrange(new(new(x, y), new Size(w, h)));
        }

        return finalSize;
    }
}