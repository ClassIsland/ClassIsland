using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using ClassIsland.Core;

namespace ClassIsland;

public static class AccentColorPicker
{
    public static Color GetCurrentAccentColor()
    {
        var app = AppBase.Current;
        var theme = GetEffectiveThemeVariant(app);

        // 使用标题实际采用的画刷，并按生效外观查询，避免 Default 命中错误的主题字典。
        if (TryGetColor(app, "AccentTextFillColorPrimaryBrush", theme, out var accent))
        {
            return accent;
        }

        if (TryGetColor(app, "AccentFillColorDefaultBrush", theme, out accent))
        {
            return accent;
        }

        return GetSystemAccentColorFallback();
    }

    // 跟随系统时 RequestedThemeVariant 为 Default，资源查询必须使用实际生效的外观。
    private static ThemeVariant GetEffectiveThemeVariant(Application app)
    {
        var requested = app.RequestedThemeVariant;
        if (requested is not null && requested != ThemeVariant.Default)
        {
            return requested;
        }

        // ActualThemeVariant 与控件解析主题资源时使用的外观一致。
        var actual = app.ActualThemeVariant;
        if (actual is not null && actual != ThemeVariant.Default)
        {
            return actual;
        }

        return app.PlatformSettings?.GetColorValues().ThemeVariant == PlatformThemeVariant.Dark
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
    }

    public static bool IsCurrentAccentColor(Color color)
    {
        var accent = GetCurrentAccentColor();
        // 需要连 Alpha 一起比：只比 RGB 会把「与当前强调色仅透明度不同」的颜色误判成默认强调色，
        // 进而被写成空字符串（= 跟随系统强调色），用户的自定义透明度会被悄悄丢掉。
        return color.A == accent.A && color.R == accent.R && color.G == accent.G && color.B == accent.B;
    }

    private static bool TryGetColor(Application app, object key, ThemeVariant? theme, out Color color)
    {
        if (app.TryFindResource(key, theme, out var resource))
        {
            switch (resource)
            {
                case Color resourceColor:
                    color = resourceColor;
                    return true;
                case ISolidColorBrush brush:
                    color = brush.Color;
                    return true;
            }
        }

        color = default;
        return false;
    }

    private static Color GetSystemAccentColorFallback()
    {
        return AppBase.Current.PlatformSettings?.GetColorValues().AccentColor1 ?? Colors.DodgerBlue;
    }
}
