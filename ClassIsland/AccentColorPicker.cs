using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ClassIsland.Core;

namespace ClassIsland;

public static class AccentColorPicker
{
    public static Color GetCurrentAccentTextColor()
    {
        if (AppBase.Current.TryFindResource("AccentTextFillColorPrimaryBrush", out var resource))
        {
            return resource switch
            {
                Color color => color,
                ISolidColorBrush brush => brush.Color,
                _ => GetSystemAccentColorFallback()
            };
        }

        return GetSystemAccentColorFallback();
    }

    private static Color GetSystemAccentColorFallback()
    {
        return AppBase.Current.PlatformSettings?.GetColorValues().AccentColor1 ?? Colors.DodgerBlue;
    }
}
