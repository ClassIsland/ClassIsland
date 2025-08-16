using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.XamlTheme;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 用于注册 Xaml 主题的 <see cref="IServiceCollection"/>扩展。
/// </summary>
public static class XamlThemeRegisterExtensions
{
    /// <summary>
    /// 注册 Xaml 主题。注册后，此主题将在主题界面中显示。
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>实例</param>
    /// <param name="uri">主题资源的 Uri</param>
    /// <param name="manifest">主题清单</param>
    /// <returns>原来的<see cref="IServiceCollection"/>实例</returns>
    public static IServiceCollection AddXamlTheme(this IServiceCollection services, Uri uri, ThemeManifest manifest)
    {
        IXamlThemeService.IntegratedThemes.Add(new ThemeInfo()
        {
            ThemeUri = uri,
            Manifest = manifest,
            IsExternal = false,
            RealBannerPath = manifest.Banner,
            IsLocal = true
        });
        return services;
    }
}