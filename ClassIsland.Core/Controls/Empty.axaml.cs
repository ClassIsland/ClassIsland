using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 空白内容占位符
/// </summary>
public class Empty : TemplatedControl
{
    public static readonly StyledProperty<IconSource> IconProperty = AvaloniaProperty.Register<Empty, IconSource>(
        nameof(Icon), new FluentIconSource("\ue262")
        {
            FontSize = 64
        });

    public IconSource Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<Empty, string>(
        nameof(Text), "啥都没有");

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}