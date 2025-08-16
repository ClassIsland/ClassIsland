using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace ClassIsland.Converters;

public class ThemeEnabledConverter : AvaloniaObject, IValueConverter
{
    private IList<string>? _source;

    public static readonly DirectProperty<ThemeEnabledConverter, IList<string>?> SourceProperty = AvaloniaProperty.RegisterDirect<ThemeEnabledConverter, IList<string>?>(
        nameof(Source), o => o.Source, (o, v) => o.Source = v);

    public IList<string>? Source
    {
        get => _source;
        set => SetAndRaise(SourceProperty, ref _source, value);
    }
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Source == null || value is not string id)
        {
            return false;
        }
        return Source.Contains(id);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}