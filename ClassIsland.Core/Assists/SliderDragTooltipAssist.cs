using System.Globalization;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace ClassIsland.Core.Assists;

public class SliderDragTooltipAssist
{
    public static readonly AttachedProperty<string> StringFormatProperty =
        AvaloniaProperty.RegisterAttached<SliderDragTooltipAssist, Slider, string>("StringFormat", "F0");

    public static void SetStringFormat(Slider obj, string value) => obj.SetValue(StringFormatProperty, value);
    public static string GetStringFormat(Slider obj) => obj.GetValue(StringFormatProperty);

    public static SliderAutoTooltipStringConverter ConverterInstance = new SliderAutoTooltipStringConverter();

    public sealed class SliderAutoTooltipStringConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 2)
            {
                return null;
            }

            if (values[0] is not double value || values[1] is not string format)
            {
                return null;
            }

            return value.ToString(format);
        }

        internal SliderAutoTooltipStringConverter()
        {
            
        }
    }
}