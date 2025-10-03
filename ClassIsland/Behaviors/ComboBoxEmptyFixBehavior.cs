using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Xaml.Interactivity;

namespace ClassIsland.Behaviors;

public class ComboBoxEmptyFixBehavior : Behavior<ComboBox>
{
    public static readonly StyledProperty<Guid> NullKeyProperty = AvaloniaProperty.Register<ComboBoxEmptyFixBehavior, Guid>(
        nameof(NullKey), defaultValue:Guid.Empty);

    public Guid NullKey
    {
        get => GetValue(NullKeyProperty);
        set => SetValue(NullKeyProperty, value);
    }

    private IDisposable? _selectedValueObserver;

    protected override void OnAttached()
    {
        if (AssociatedObject == null)
        {
            return;
        }

        _selectedValueObserver?.Dispose();
        _selectedValueObserver = AssociatedObject.GetObservable(SelectingItemsControl.SelectedValueProperty)
            .Skip(1)
            .Subscribe(OnSelectedValueChanged);
        
        UpdateSelectedValue();
        base.OnAttached();
    }

    private void OnSelectedValueChanged(object? obj)
    {
        UpdateSelectedValue();
    }

    private void UpdateSelectedValue()
    {
        if (AssociatedObject == null)
        {
            return;
        }

        if (Equals(AssociatedObject.SelectedValue, NullKey))
        {
            AssociatedObject.SelectedIndex = -1;
        }
    }

    protected override void OnDetaching()
    {
        _selectedValueObserver?.Dispose();
        base.OnDetaching();
    }
}