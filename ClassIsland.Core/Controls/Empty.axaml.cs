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
    public static readonly StyledProperty<FAIconSource> IconProperty = AvaloniaProperty.Register<Empty, FAIconSource>(
        nameof(Icon), new FluentIconSource("\ue262")
        {
            FontSize = 64
        });

    public FAIconSource Icon
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

    public static readonly StyledProperty<double> IconHeightProperty = AvaloniaProperty.Register<Empty, double>(
        nameof(IconHeight), 64.0);

    public double IconHeight
    {
        get => GetValue(IconHeightProperty);
        set => SetValue(IconHeightProperty, value);
    }

    public static readonly StyledProperty<double> IconWidthProperty = AvaloniaProperty.Register<Empty, double>(
        nameof(IconWidth), 64.0);

    public double IconWidth
    {
        get => GetValue(IconWidthProperty);
        set => SetValue(IconWidthProperty, value);
    }
}