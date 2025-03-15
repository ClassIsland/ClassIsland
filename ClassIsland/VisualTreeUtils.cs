using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace ClassIsland;

public static class VisualTreeUtils
{
    public static List<T> FindParentVisuals<T>(DependencyObject? o) where T: DependencyObject
    {
        if (o is null)
        {
            return [];
        }
        var parent = VisualTreeHelper.GetParent(o);
        var r = new List<T>();
        while (parent != null)
        {
            if (parent is T p)
            {
                r.Add(p);
            }
            parent = VisualTreeHelper.GetParent(parent);
        }

        return r;
    }

    public static T? FindChildVisualByName<T>(FrameworkElement parent, string name) where T : FrameworkElement
    {
        if (parent is T element && element.Name == name)
        {
            return element;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is not FrameworkElement childElement)
            {
                continue;
            }
            // 递归查找子控件
            var result = FindChildVisualByName<T>(childElement, name);
            if (result != null)
                return result;
        }

        return null;
    }
}