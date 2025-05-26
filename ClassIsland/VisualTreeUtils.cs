using System.Collections.Generic;
using System.Text;
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

    public static string DumpVisualTree(FrameworkElement element)
    {
        var elements = new List<string>();
        DumpVisualTreeInternal(element, elements, 0);
        return string.Join("\n", elements);
    }

    private static void DumpVisualTreeInternal(DependencyObject element, List<string> elementList, int depth)
    {
        if (element is not FrameworkElement fe)
        {
            return;
        }
        var sb = new StringBuilder();
        for (int i = 0; i < depth; i++)
        {
            sb.Append("| ");
        }
        if (depth > 0)
        {
            sb.Append('>');
        }

        sb.Append($"{fe.Name}({element.GetHashCode()}) [{element.GetType()}]");
        elementList.Add(sb.ToString());

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            DumpVisualTreeInternal(VisualTreeHelper.GetChild(element, i), elementList, depth + 1);
        }
    }
}