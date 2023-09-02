using System;
using System.Collections;
using System.Reflection;
using System.Windows;

namespace ClassIsland;

public static class DependencyPropertyHelper
{
    public static void ForceOverwriteDependencyPropertyDefaultValue(DependencyProperty prop, object value)
    {
        var metadata = prop.DefaultMetadata;
        var mdType = metadata.GetType();
        var defaultField = mdType.GetField("_defaultValue", BindingFlags.NonPublic | BindingFlags.Instance);
        if (defaultField == null)
        {
            return;
        }
        defaultField.SetValue(metadata, value);
    }
}