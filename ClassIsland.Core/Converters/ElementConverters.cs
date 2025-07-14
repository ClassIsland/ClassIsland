using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace ClassIsland.Core.Converters;

public class ElementConverters
{
    private static Border? _border;

    public static readonly FuncValueConverter<object?, object> ControlPreventNullConverter = new(x =>
    {
        _border ??= new Border();
        return x ?? _border;
    });
}