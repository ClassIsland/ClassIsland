using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.Xaml.Behaviors;

namespace ClassIsland.Behaviors;

public class SliderManipulationBehavior : Behavior<Slider>
{
    protected override void OnAttached()
    {
        AssociatedObject.MouseEnter += AssociatedObjectOnMouseEnter;
        AssociatedObject.MouseUp += AssociatedObjectOnMouseUp;
        AssociatedObject.MouseLeave += AssociatedObjectOnMouseLeave;
        base.OnAttached();
    }

    private void AssociatedObjectOnMouseLeave(object sender, MouseEventArgs e)
    {
        VisualTreeUtils.FindParentVisuals<ScrollViewer>(AssociatedObject).ForEach(i => i.IsManipulationEnabled = true);
    }

    private void AssociatedObjectOnMouseEnter(object sender, MouseEventArgs e)
    {
        VisualTreeUtils.FindParentVisuals<ScrollViewer>(AssociatedObject).ForEach(i => i.IsManipulationEnabled = false);
    }

    private void AssociatedObjectOnMouseUp(object sender, MouseButtonEventArgs e)
    {
        VisualTreeUtils.FindParentVisuals<ScrollViewer>(AssociatedObject).ForEach(i => i.IsManipulationEnabled = true);
    }

    private void AssociatedObjectOnMouseDown(object sender, MouseButtonEventArgs e)
    {
        
    }
}