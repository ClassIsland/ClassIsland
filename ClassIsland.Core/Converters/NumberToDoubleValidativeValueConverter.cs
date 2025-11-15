using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace ClassIsland.Core.Converters;

/// <summary>
/// 
/// </summary>
public class NotNaNConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return System.Convert.ChangeType(value, typeof(double));
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double d)
        {
            return value ?? BindingOperations.DoNothing;
        }

        return double.IsFinite(d) ? d : BindingOperations.DoNothing;
    }

    private NotNaNConverter()
    {
        
    }

    /// <summary>
    /// 
    /// </summary>
    public static NotNaNConverter Instance { get; } = new();
}
