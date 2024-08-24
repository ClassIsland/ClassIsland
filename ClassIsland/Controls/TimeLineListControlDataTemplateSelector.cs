using System.Windows;
using System.Windows.Controls;

using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls;

public class TimeLineListControlDataTemplateSelector : DataTemplateSelector
{
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        var e = (FrameworkElement)container;
        var o = (TimeLayoutItem)item;
        var key = o.TimeType == 2 ? "DataTemplateSeparator" : "DataTemplateTimePoint";
        return (DataTemplate)e.FindResource(key);
    }
}