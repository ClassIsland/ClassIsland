using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ClassIsland.Converters;

public class StringToFontFamilyConverter : IValueConverter
{
    public const string UseSharedDefaultParameter = "UseSharedDefault";

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => ResolveFontFamily(value as string, parameter as string == UseSharedDefaultParameter);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => ToFontFamilyKey(value);

    /// <summary>
    /// 将字体配置字符串解析为 <see cref="FontFamily"/>。任何解析失败都回退到默认字体，绝不抛出异常。
    /// </summary>
    /// <remarks>
    /// 此方法被用于 XAML 绑定，而转换器抛出的异常会直接导致应用崩溃，因此必须保证不抛出。
    /// 注意：回退路径自身也做了保护，避免回退时再次抛出异常（例如默认字体资源缺失）。
    /// </remarks>
    private static object ResolveFontFamily(string? fontFamilyKey, bool useSharedDefault)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(fontFamilyKey))
            {
                var fontFamily = FontFamily.Parse(fontFamilyKey);
                return !useSharedDefault || fontFamily.ToString() == MainWindow.DefaultFontFamily.ToString()
                    ? fontFamily
                    : MainWindow.DefaultFontFamily;
            }
        }
        catch (Exception)
        {
            // 忽略解析失败，走下方回退逻辑。
        }

        try
        {
            return useSharedDefault ? MainWindow.DefaultFontFamily : FontFamily.Parse(MainWindow.DefaultFontFamilyKey);
        }
        catch (Exception)
        {
            return FontFamily.Default;
        }
    }

    /// <summary>
    /// 将绑定值还原为字体配置字符串。
    /// </summary>
    /// <remarks>
    /// 使用 <see cref="IValueConverter"/> 做 TwoWay 绑定时，控件在 ItemsSource 刷新等场景下会把
    /// SelectedItem 置空并回写，因此 value 完全可能是 null 或非 <see cref="FontFamily"/> 类型。
    /// 此处必须做类型与空值防护，否则会因强制转换或空引用导致界面崩溃。
    /// </remarks>
    private static string ToFontFamilyKey(object? value)
    {
        if (value is not FontFamily fontFamily)
        {
            return string.Empty;
        }

        try
        {
            if (fontFamily.Key != null)
            {
                return fontFamily.Key.ToString().Replace("compositefont:", "");
            }

            return fontFamily.ToString();
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
