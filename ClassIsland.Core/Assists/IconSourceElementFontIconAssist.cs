using Avalonia;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Assists;

/// <summary>
/// 适用于 <see cref="FAIconSourceElement"/> 中 <see cref="FAFontIcon"/> 的助手类
/// </summary>
public class IconSourceElementFontIconAssist
{
    public static readonly AttachedProperty<bool> IsFontSizeRepairedProperty =
        AvaloniaProperty.RegisterAttached<IconSourceElementFontIconAssist, FAFontIcon, bool>("IsFontSizeRepaired");

    public static void SetIsFontSizeRepaired(FAFontIcon obj, bool value) => obj.SetValue(IsFontSizeRepairedProperty, value);
    public static bool GetIsFontSizeRepaired(FAFontIcon obj) => obj.GetValue(IsFontSizeRepairedProperty);

    static IconSourceElementFontIconAssist()
    {
        IsFontSizeRepairedProperty.Changed.AddClassHandler<FAFontIcon>(IsFontSizeRepairedPropertyChanged);
    }

    private static void IsFontSizeRepairedPropertyChanged(FAFontIcon arg1, AvaloniaPropertyChangedEventArgs arg2)
    {
        arg1.ClearValue(FAFontIcon.FontSizeProperty);
    }
}