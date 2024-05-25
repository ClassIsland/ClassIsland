using System.Reflection;
using System.Windows;

namespace ClassIsland;

public static class DependencyPropertyHelper
{
    public static void ForceOverwriteDependencyPropertyDefaultValue(DependencyProperty prop, object value)
    {
        var metadata = prop.DefaultMetadata;
        var mdType = typeof(PropertyMetadata);
        var defaultField = mdType.GetField("_defaultValue", BindingFlags.NonPublic | BindingFlags.Instance);
        if (defaultField == null)
        {
            return;
        }
        defaultField.SetValue(metadata, value);
    }

    public static Setter? FindSetter(SetterBaseCollection setters, DependencyProperty prop)
    {
        foreach (var setter in setters)
        {
            if (setter is not Setter ss) continue;
            if (ss.Property == prop)
            {
                return ss;
            }
            
        }
        return null;
    }
}