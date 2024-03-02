using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace ClassIsland;

public static class VisualTreeUtils
{
    public static List<T> FindParentVisuals<T>(DependencyObject o) where T: DependencyObject
    {
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
}