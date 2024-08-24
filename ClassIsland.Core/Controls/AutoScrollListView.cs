using System.Collections.Specialized;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace ClassIsland.Core.Controls;

public class AutoScrollListView : ListView
{
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            var scroll = (new ListViewAutomationPeer(this).GetPattern(PatternInterface.Scroll) as ScrollViewerAutomationPeer).Owner as ScrollViewer;
            if (Math.Abs(scroll.ScrollableHeight - scroll.VerticalOffset) < 0.1)
                scroll.ScrollToBottom();
        }
        base.OnItemsChanged(e);
    }
}