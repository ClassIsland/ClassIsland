using Avalonia;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Assists;

/// <summary>
/// 适用于 <see cref="IconSourceElement"/> 中 <see cref="FontIcon"/> 的助手类
/// </summary>
public class IconSourceElementFontIconAssist
{
    public static readonly AttachedProperty<bool> IsFontSizeRepairedProperty =
        AvaloniaProperty.RegisterAttached<IconSourceElementFontIconAssist, FontIcon, bool>("IsFontSizeRepaired");

    public static void SetIsFontSizeRepaired(FontIcon obj, bool value) => obj.SetValue(IsFontSizeRepairedProperty, value);
    public static bool GetIsFontSizeRepaired(FontIcon obj) => obj.GetValue(IsFontSizeRepairedProperty);

    static IconSourceElementFontIconAssist()
    {
        IsFontSizeRepairedProperty.Changed.AddClassHandler<FontIcon>(IsFontSizeRepairedPropertyChanged);
    }

    private static void IsFontSizeRepairedPropertyChanged(FontIcon arg1, AvaloniaPropertyChangedEventArgs arg2)
    {
        arg1.ClearValue(FontIcon.FontSizeProperty);
    }
}