using System;
using System.Collections.ObjectModel;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using Avalonia.Xaml.Interactivity;

namespace ClassIsland.Behaviors;

public class AllCollectionBehavior : Behavior<ListBoxItem>
{
    public static readonly StyledProperty<ObservableCollection<string>> AllCollectionProperty = AvaloniaProperty.Register<AllCollectionBehavior, ObservableCollection<string>>(
        nameof(AllCollection));
    public ObservableCollection<string> AllCollection
    {
        get => GetValue(AllCollectionProperty);
        set => SetValue(AllCollectionProperty, value);
    
    }

    public static readonly StyledProperty<string> CurrentValueProperty = AvaloniaProperty.Register<AllCollectionBehavior, string>(
        nameof(CurrentValue));
    public string CurrentValue
    {
        get => GetValue(CurrentValueProperty);
        set => SetValue(CurrentValueProperty, value);
    
    }
    
    private IDisposable? Subscription { get; set; }

    protected override void OnAttached()
    {
        Subscription = AssociatedObject?.GetObservable(ListBoxItem.IsSelectedProperty)
            .Subscribe(new AnonymousObserver<bool>(ListBoxItemIsSelectedChanged));
        UpdateValue();
        base.OnAttached();
    }

    private void ListBoxItemIsSelectedChanged(bool value)
    {
        if (value)
        {
            if (!AllCollection.Contains(CurrentValue))
            {
                AllCollection.Add(CurrentValue);
            }
        }
        else
        {
            if (AllCollection.Contains(CurrentValue))
            {
                AllCollection.Remove(CurrentValue);
            }
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject == null)
        {
            return;
        }
        Subscription?.Dispose();
        Subscription = null;
        base.OnDetaching();
    }

    private void UpdateValue()
    {
        if (AssociatedObject == null || AllCollection == null || CurrentValue == null)
            return;
        AssociatedObject.IsSelected = AllCollection.Contains(CurrentValue);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == AllCollectionProperty)
        {
            UpdateValue();
        }
        base.OnPropertyChanged(e);
    }
    
}