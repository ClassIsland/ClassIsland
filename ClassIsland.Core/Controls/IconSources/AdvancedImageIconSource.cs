using Avalonia;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls.IconSources;

/// <summary>
/// 高级图像的图标源
/// </summary>
public class AdvancedImageIconSource : IconSource
{
    public static readonly StyledProperty<string> UriProperty = AvaloniaProperty.Register<AdvancedImageIconSource, string>(
        nameof(Uri));

    public string Uri
    {
        get => GetValue(UriProperty);
        set => SetValue(UriProperty, value);
    }
    
    static AdvancedImageIconSource()
    {
        IconHelpers.RegisterCustomIconSourceFactory(typeof(AdvancedImageIconSource), x => x switch
        {
            AdvancedImageIconSource iconSource => new AdvancedImageIcon()
            {
                IconSource = iconSource
            },
            _ => null
        });
    }
}