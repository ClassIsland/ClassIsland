using System.Globalization;
using System.Windows.Data;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Shared;

namespace ClassIsland.Core.Abstractions.Converters;

/// <summary>
/// 用于在绑定中通过<seealso cref="string"/>类型的键访问以<seealso cref="string"/>为键，以<seealso cref="T"/>类型为值的字典的值的多项值转换器。
/// 使用时需要以此抽象类为基类新建一个类，并将字典值的类型作为继承时的类型参数。（<a href="https://learn.microsoft.com/zh-cn/dotnet/desktop/xaml-services/generics#generics-support-in-wpf">为什么不直接在XAML中传入泛型参数</a>）
/// <br/>
/// 在转换时，第一个传入的绑定指向要访问的字典，第二个值指向要访问的键。如果获取值失败将返回 null。
/// 此转换器不能反向转换。
/// </summary>
/// <typeparam name="T">字典值类型</typeparam>
public abstract class DictionaryValueAccessConverter<T> : IMultiValueConverter
{
    /// <inheritdoc />
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
        {
            return null;
        }
        var dict = values[0] as IDictionary<string, T>;
        var key = values[1] as string;
        if (dict?.TryGetValue(key ?? "", out var o) == true)
        {
            return o;
        }

        return null;
    }

    /// <inheritdoc />
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[] { };
    }
}