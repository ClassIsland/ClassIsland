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

        // 色块必须与分组标题实际渲染出的颜色一致：标题用的是 AccentTextFillColorPrimaryBrush，
        // 该画刷按当前外观解析（深色外观下是系统强调色的浅色变体，浅色外观下是深色变体）。
        // 资源查询必须显式传入有效外观，避免用 Default 查询时选中错误的主题字典。
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

    /// <summary>
    /// 获取当前真正生效的外观变体。跟随系统时 <see cref="Application.RequestedThemeVariant"/> 会被
    /// <c>ThemeService.SetTheme</c> 显式设成 <see cref="ThemeVariant.Default"/>，此时需要回落到真实
    /// 生效的外观，否则按主题字典查颜色会命中浅色字典，拿到明显偏深的强调色。
    /// </summary>
    private static ThemeVariant GetEffectiveThemeVariant(Application app)
    {
        var requested = app.RequestedThemeVariant;
        if (requested is not null && requested != ThemeVariant.Default)
        {
            return requested;
        }

        // 跟随系统时 Application.ActualThemeVariant 实测会解析成真实生效的外观（Dark/Light），
        // 比直接问系统外观更贴近控件自身的解析结果。
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
