using System.Collections.Specialized;
using System.Windows.Controls;

namespace ClassIsland.Core.Controls;

public class AutoScrollListView : ListView
{
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (var i in e.NewItems)
            {
                ScrollIntoView(i);
            }
        }
        base.OnItemsChanged(e);
    }
}