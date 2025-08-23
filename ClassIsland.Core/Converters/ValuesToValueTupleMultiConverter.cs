using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
namespace ClassIsland.Core.Converters;

public class ValuesToValueTupleMultiConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        return values.Count switch
        {
            1 => ValueTuple.Create(values[0]),
            2 => ValueTuple.Create(values[0], values[1]),
            3 => ValueTuple.Create(values[0], values[1], values[2]),
            4 => ValueTuple.Create(values[0], values[1], values[2], values[3]),
            5 => ValueTuple.Create(values[0], values[1], values[2], values[3], values[4]),
            6 => ValueTuple.Create(values[0], values[1], values[2], values[3], values[4], values[5]),
            7 => ValueTuple.Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6]),
            _ => BindingOperations.DoNothing
        };
    }
}