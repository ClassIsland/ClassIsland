using System;
using System.Windows.Controls;

using Microsoft.Xaml.Behaviors;

namespace ClassIsland;

public class AutoScrollBehavior : Behavior<ListBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        this.AssociatedObject.SelectionChanged += new SelectionChangedEventHandler(AssociatedObject_SelectionChanged);
    }
    void AssociatedObject_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox)
        {
            if (listBox.SelectedItem != null)
            {
                listBox .Dispatcher.BeginInvoke((Action)delegate
                {
                    listBox.UpdateLayout();
                    listBox.ScrollIntoView(listBox.SelectedItem);//在这里使用一的方法
                });
            }
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        this.AssociatedObject.SelectionChanged -= new SelectionChangedEventHandler(AssociatedObject_SelectionChanged);
    }
}