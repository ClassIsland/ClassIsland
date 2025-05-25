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
        var key = o.TimeType switch
        {
            0 => "DataTemplateTimePoint",
            1 => "DataTemplateTimePoint",
            2 => "DataTemplateSeparator",
            3 => "DataTemplateAction",
            _ => "DataTemplateTimePoint"
        };
        return (DataTemplate)e.FindResource(key);
    }
}