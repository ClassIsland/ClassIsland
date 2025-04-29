using System.Collections.ObjectModel;
using ClassIsland.Core.Models.XamlTheme;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// XAML 主题服务
/// </summary>
/// <remarks>
/// 注意将此服务与 <see cref="IThemeService"/> 区分开来。后者负责整个应用的主题，而此服务负责主界面的自定义 XAML 主题。
/// </remarks>
public interface IXamlThemeService
{
    /// <summary>
    /// 已加载的主题列表
    /// </summary>
    public ObservableCollection<ThemeInfo> Themes { get; }

    /// <summary>
    /// 重载全部主题
    /// </summary>
    void LoadAllThemes();

    /// <summary>
    /// 加载指定主题
    /// </summary>
    /// <param name="themePath">主题路径</param>
    void LoadTheme(string themePath);
}