using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Xaml.Behaviors;

namespace ClassIsland.Behaviors;

public class AllCollectionBehavior : Behavior<ListBoxItem>
{
    public static readonly DependencyProperty AllCollectionProperty = DependencyProperty.Register(
        nameof(AllCollection), typeof(ObservableCollection<string>), typeof(AllCollectionBehavior), new PropertyMetadata(default(ObservableCollection<string>)));

    public ObservableCollection<string> AllCollection
    {
        get { return (ObservableCollection<string>)GetValue(AllCollectionProperty); }
        set { SetValue(AllCollectionProperty, value); }
    }

    public static readonly DependencyProperty CurrentValueProperty = DependencyProperty.Register(
        nameof(CurrentValue), typeof(string), typeof(AllCollectionBehavior), new PropertyMetadata(default(string)));

    public string CurrentValue
    {
        get { return (string)GetValue(CurrentValueProperty); }
        set { SetValue(CurrentValueProperty, value); }
    }

    protected override void OnAttached()
    {
        AssociatedObject.Selected += AssociatedObjectOnSelected;
        AssociatedObject.Unselected += AssociatedObjectOnUnselected;
        UpdateValue();
        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.Selected -= AssociatedObjectOnSelected;
        AssociatedObject.Unselected -= AssociatedObjectOnUnselected;
        base.OnDetaching();
    }

    private void UpdateValue()
    {
        if (AssociatedObject == null || AllCollection == null || CurrentValue == null)
            return;
        AssociatedObject.IsSelected = AllCollection.Contains(CurrentValue);
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.Property == AllCollectionProperty)
        {
            UpdateValue();
        }
        base.OnPropertyChanged(e);
    }

    private void AssociatedObjectOnUnselected(object sender, RoutedEventArgs e)
    {
        if (AllCollection.Contains(CurrentValue))
        {
            AllCollection.Remove(CurrentValue);
        }
    }

    private void AssociatedObjectOnSelected(object sender, RoutedEventArgs e)
    {
        if (!AllCollection.Contains(CurrentValue))
        {
            AllCollection.Add(CurrentValue);
        }
    }
}