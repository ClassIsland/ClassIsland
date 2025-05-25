using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

/// <inheritdoc />
public class InvertBooleanAndToVisibilityMultiConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => values.All(x => x.Equals(true)) ? Visibility.Collapsed : Visibility.Visible;

    /// <inheritdoc />
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}