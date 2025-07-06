using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Xaml.Interactivity;
using ClassIsland.Controls.TimeLine;

namespace ClassIsland.Behaviors;

public class TimeLineListItemBehavior : Behavior<Control>
{

    public static readonly StyledProperty<IControlTemplate> ThumbTemplateProperty = AvaloniaProperty.Register<TimeLineListItemBehavior, IControlTemplate>(
        nameof(ThumbTemplate));

    public IControlTemplate ThumbTemplate
    {
        get => GetValue(ThumbTemplateProperty);
        set => SetValue(ThumbTemplateProperty, value);
    }

    public static readonly StyledProperty<TimeLineListControl> ParentListBoxProperty = AvaloniaProperty.Register<TimeLineListItemBehavior, TimeLineListControl>(
        nameof(ParentListBox));

    public TimeLineListControl ParentListBox
    {
        get => GetValue(ParentListBoxProperty);
        set => SetValue(ParentListBoxProperty, value);
    }

    private ListBoxItem? _listBoxItem;

    public static readonly DirectProperty<TimeLineListItemBehavior, ListBoxItem?> ListBoxItemProperty = AvaloniaProperty.RegisterDirect<TimeLineListItemBehavior, ListBoxItem?>(
        nameof(ListBoxItem), o => o.ListBoxItem, (o, v) => o.ListBoxItem = v);

    public ListBoxItem? ListBoxItem
    {
        get => _listBoxItem;
        set => SetAndRaise(ListBoxItemProperty, ref _listBoxItem, value);
    }

    private Control? _adorner;
    private AdornerLayer? _layer;

    protected override void OnAttached()
    {
        if (AssociatedObject != null)
        {
            this.GetObservable(ListBoxItemProperty).Subscribe(_ => UpdateListBoxItemObserver());
        }
        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        RemoveAdorner();
        base.OnDetaching();
    }

    private void UpdateListBoxItemObserver()
    {
        if (ListBoxItem == null)
        {
            return;
        }
        RemoveAdorner();
        ListBoxItem.Unloaded += (_, _) => RemoveAdorner();
        ListBoxItem.GetObservable(ListBoxItem.IsSelectedProperty).Subscribe(v =>
        {
            CheckAdorner(v);
        });
        if (AssociatedObject != null)
        {
            AssociatedObject.AttachedToVisualTree += (sender, args) => CheckAdorner(ListBoxItem.IsSelected);
            AssociatedObject.DetachedFromVisualTree += (o, _) => RemoveAdorner(o);
        }
        if (ListBoxItem.IsSelected) AttachAdorner();
    }

    private void CheckAdorner(bool v)
    {
        if (v)
        {
            AttachAdorner();
        }
        else
        {
            RemoveAdorner();
        }
    }

    private void AssociatedObjectOnUnselected(object sender, RoutedEventArgs e)
    {
        RemoveAdorner();
    }

    private void RemoveAdorner(object? o=null)
    {
        var owner = AssociatedObject ?? o as Visual;
        if (owner == null )
        {
            return;
        }
        if (_adorner != null)
        {
            _layer?.Children.Remove(_adorner);
            AdornerLayer.SetAdornedElement(_adorner, null);
            _adorner = null;
        }
    }

    private void AssociatedObjectOnSelected(bool v)
    {
        AttachAdorner();
    }

    private void AttachAdorner()
    {
        if (AssociatedObject == null)
        {
            return;
        }
        var layer = _layer = AdornerLayer.GetAdornerLayer(AssociatedObject);
        if (layer == null || _adorner != null)
        {
            return;
        }
        
        _adorner = new TemplatedControl()
        {
            Template = ThumbTemplate,
            ClipToBounds = false,
            DataContext = ParentListBox
        };
        AdornerLayer.SetIsClipEnabled(_adorner, true);
        layer?.Children.Add(_adorner);
        AdornerLayer.SetAdornedElement(_adorner, AssociatedObject);
    }
}