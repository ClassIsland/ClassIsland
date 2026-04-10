using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace ClassIsland.Core.Converters;

public class ElementConverters
{
    public static readonly FuncValueConverter<object?, object> ControlPreventNullConverter = new(x =>
    {
        return x ?? new Border();
    });
}
