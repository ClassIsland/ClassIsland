using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Abstractions.Converters;

/// <summary>
/// 用于在绑定中将指定类型的enum转换为<see cref="int"/>。
/// 使用时需要以此抽象类为基类新建一个类，并将指定的enum类型作为继承时的类型参数。（<a href="https://learn.microsoft.com/zh-cn/dotnet/desktop/xaml-services/generics#generics-support-in-wpf">为什么不直接在XAML中传入泛型参数</a>）
/// </summary>
/// <typeparam name="T">enum值类型</typeparam>
public abstract class EnumToIntConverter<T> : IValueConverter where T : Enum
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return -1;
        return ((T)value).GetHashCode();
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        var v = value as int?;
        if (v == null) return default(T);
        return (T)(object)v;
    }
}