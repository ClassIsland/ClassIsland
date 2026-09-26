using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace ClassIsland.Core.Converters;

/// <summary>
/// 通用 enum 到 int 值转换器
/// </summary>
public class EnumToIntConverter : IValueConverter
{
    /// <summary>
    /// <see cref="EnumToIntConverter"/> 的实例
    /// </summary>
    public static EnumToIntConverter Instance { get; } = new();
    
    /// <inheritdoc />
    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is null)
        {
            return Nullable.GetUnderlyingType(targetType) is not null
                ? null
                : BindingOperations.DoNothing;
        }

        if (value is not Enum)
            return BindingOperations.DoNothing;

        var actualTargetType =
            Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (actualTargetType != typeof(int))
            return BindingOperations.DoNothing;

        try
        {
            return System.Convert.ToInt32(value, culture);
        }
        catch (Exception exception) when (
            exception is InvalidCastException or
                FormatException or
                OverflowException)
        {
            return BindingOperations.DoNothing;
        }
    }

    /// <inheritdoc />
    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        var enumType =
            Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (!enumType.IsEnum)
            return BindingOperations.DoNothing;

        if (value is null)
        {
            return Nullable.GetUnderlyingType(targetType) is not null
                ? null
                : BindingOperations.DoNothing;
        }

        try
        {
            var intValue = System.Convert.ToInt32(value, culture);
            var enumValue = Enum.ToObject(enumType, intValue);
            // 选择控件在初始化选项时可能短暂产生 -1，不能将未定义值写回枚举。
            return Enum.IsDefined(enumType, enumValue)
                ? enumValue
                : BindingOperations.DoNothing;
        }
        catch (Exception exception) when (
            exception is InvalidCastException or
                FormatException or
                OverflowException or
                ArgumentException)
        {
            return BindingOperations.DoNothing;
        }
    }

    private EnumToIntConverter()
    {
        
    }
}
