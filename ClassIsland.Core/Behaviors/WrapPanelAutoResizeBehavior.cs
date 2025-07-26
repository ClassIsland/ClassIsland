using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace ClassIsland.Core.Behaviors;

/// <summary>
/// 让 <see cref="WrapPanel"/> 自动调整元素宽度的行为
/// </summary>
public class WrapPanelAutoResizeBehavior : Behavior<WrapPanel>
{

    public static readonly StyledProperty<double> TargetWidthProperty = AvaloniaProperty.Register<WrapPanelAutoResizeBehavior, double>(
        nameof(TargetWidth));

    public double TargetWidth
    {
        get => GetValue(TargetWidthProperty);
        set => SetValue(TargetWidthProperty, value);
    }
    
    /// <inheritdoc />
    protected override void OnAttached()
    {
        if (AssociatedObject is null)
        {
            return;
        }
        AssociatedObject.SizeChanged += AssociatedObjectOnSizeChanged;
        
        base.OnAttached();
    }
    
    /// <inheritdoc />
    protected override void OnDetaching()
    {
        if (AssociatedObject is null)
        {
            return;
        }
        AssociatedObject.SizeChanged -= AssociatedObjectOnSizeChanged;
        base.OnDetaching();
    }
    
    private void AssociatedObjectOnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (AssociatedObject == null)
        {
            return;
        }

        var cols = Math.Round(Math.Max(1, AssociatedObject.Bounds.Width) / Math.Max(1, TargetWidth));
        if (cols <= 1)
        {
            cols = 1;
        }

        var width = AssociatedObject.Bounds.Width / cols;
        AssociatedObject.ItemWidth = width;
    }
}
