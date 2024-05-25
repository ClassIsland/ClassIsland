using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

using ClassIsland.Controls;

using Microsoft.Xaml.Behaviors;

namespace ClassIsland.Behaviors;

public class ExcelSelectingTextBoxBehavior : Behavior<ExcelSelectionTextBox>
{
    public static readonly DependencyProperty AdornerTemplateProperty = DependencyProperty.Register(
        nameof(AdornerTemplate), typeof(ControlTemplate), typeof(ExcelSelectingTextBoxBehavior), new PropertyMetadata(default(ControlTemplate)));

    public ControlTemplate AdornerTemplate
    {
        get { return (ControlTemplate)GetValue(AdornerTemplateProperty); }
        set { SetValue(AdornerTemplateProperty, value); }
    }

    private Adorner? _adorner;

    protected override void OnAttached()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.OnEnterSelecting += AssociatedObjectOnEnterSelecting;
            AssociatedObject.OnExitSelecting += AssociatedObjectOnExitSelecting;
        }
        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        RemoveAdorner();
        base.OnDetaching();
    }

    private void AssociatedObjectOnExitSelecting(object? sender, EventArgs e)
    {
        RemoveAdorner();
    }

    private void RemoveAdorner()
    {
        if (AssociatedObject == null)
        {
            return;
        }
        if (_adorner != null)
        {
            var layer = AdornerLayer.GetAdornerLayer(AssociatedObject);
            layer?.Remove(_adorner);
            _adorner = null;
        }
    }

    private void AssociatedObjectOnEnterSelecting(object? sender, EventArgs e)
    {
        if (AssociatedObject == null)
        {
            return;
        }
        var layer = AdornerLayer.GetAdornerLayer(AssociatedObject);
        if (_adorner != null)
        {
            return;
        }
        _adorner = new TimeLineListItemAdorner(AssociatedObject, AdornerTemplate);
        layer?.Add(_adorner);
    }
}