using System.Globalization;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using ClassIsland.Converters;
using Xunit;

namespace ClassIsland.Tests;

/// <summary>
/// <see cref="StringToFontFamilyConverter"/> 的回归测试。
///
/// 背景：该转换器在 <c>AppearanceSettingsPage.axaml</c> 中用在 TwoWay 绑定上
/// （ComboBox.SelectedItem）。控件在 ItemsSource 刷新等场景下会把 SelectedItem 置空并回写，
/// 于是 ConvertBack 会收到 null。原实现直接做强转 <c>(FontFamily)value</c>，
/// 使 InvalidCastException 从绑定链抛出并导致应用崩溃（对应上游 #1971 / #1951 / #1986）。
///
/// 因此本文件锁定的核心契约是：<b>转换器在任何输入下都不得抛出异常</b>。
///
/// 说明：FontFamily 依赖 Avalonia 的 FontManager，因此使用 [AvaloniaFact] 建立 headless 会话。
/// </summary>
public class StringToFontFamilyConverterTests
{
    private static readonly StringToFontFamilyConverter Converter = new();

    private static string ConvertBack(object? value) =>
        (string)Converter.ConvertBack(value, typeof(string), null, CultureInfo.InvariantCulture);

    private static object Convert(object? value, string? parameter = null) =>
        Converter.Convert(value, typeof(FontFamily), parameter, CultureInfo.InvariantCulture);

    // ---------- ConvertBack：崩溃防护（核心回归）----------

    [AvaloniaFact]
    public void ConvertBack_Null_DoesNotThrow_AndReturnsEmptyString()
    {
        // 这正是导致 #1971 / #1951 / #1986 的输入。
        var result = ConvertBack(null);

        Assert.Equal(string.Empty, result);
    }

    [AvaloniaTheory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(3.14)]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData('c')]
    public void ConvertBack_NonFontFamilyValues_DoNotThrow(object? value)
    {
        var result = ConvertBack(value);

        Assert.NotNull(result);
    }

    [AvaloniaFact]
    public void ConvertBack_NonNullableInput_MatchesDeclaredContract()
    {
        // ConvertBack 声明返回 string，任何输入都不得返回 null，
        // 否则会把 null 写回 Settings.MainWindowFont。
        Assert.NotNull(ConvertBack(null));
        Assert.NotNull(ConvertBack("not a font"));
        Assert.NotNull(ConvertBack(123));
    }

    // ---------- ConvertBack：正常路径行为保持 ----------

    [AvaloniaFact]
    public void ConvertBack_SystemFont_ReturnsNonEmptyFamilyName()
    {
        var result = ConvertBack(new FontFamily("Arial"));

        Assert.False(string.IsNullOrEmpty(result));
        Assert.Contains("Arial", result, StringComparison.OrdinalIgnoreCase);
    }

    [AvaloniaFact]
    public void ConvertBack_StripsCompositeFontPrefix()
    {
        // 复合字体的 Key 带 "compositefont:" 前缀，配置里不应存该前缀。
        var composite = FontFamily.Parse("Arial, Times New Roman");

        var result = ConvertBack(composite);

        Assert.DoesNotContain("compositefont:", result);
        Assert.False(string.IsNullOrEmpty(result));
    }

    // ---------- Convert：回退路径自身不得抛出 ----------

    [AvaloniaTheory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!! not a valid font family !!!")]
    [InlineData("avares://NoSuchAssembly/Assets/Fonts/#NoSuchFont")]
    public void Convert_InvalidInput_DoesNotThrow_AndReturnsFontFamily(string? input)
    {
        var result = Convert(input);

        Assert.IsAssignableFrom<FontFamily>(result);
    }

    [AvaloniaFact]
    public void Convert_ValidSystemFont_ReturnsThatFamily()
    {
        var result = Convert("Arial");

        var fontFamily = Assert.IsAssignableFrom<FontFamily>(result);
        Assert.Contains("Arial", fontFamily.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [AvaloniaFact]
    public void Convert_UseSharedDefaultParameter_DoesNotThrow()
    {
        var result = Convert("Arial", StringToFontFamilyConverter.UseSharedDefaultParameter);

        Assert.IsAssignableFrom<FontFamily>(result);
    }

    // ---------- 往返一致性 ----------

    [AvaloniaFact]
    public void RoundTrip_SystemFont_IsStable()
    {
        var original = new FontFamily("Arial");

        var key = (string)Converter.ConvertBack(original, typeof(string), null, CultureInfo.InvariantCulture);
        var roundTripped = Converter.Convert(key, typeof(FontFamily), null, CultureInfo.InvariantCulture);

        Assert.IsAssignableFrom<FontFamily>(roundTripped);
    }
}
