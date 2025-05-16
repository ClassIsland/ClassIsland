using System.Collections.ObjectModel;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Core.Models.XamlTheme;
using ClassIsland.Shared;

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

    /// <summary>
    /// 已将主题仓库与本地主题合并的全部主题
    /// </summary>
    public ObservableDictionary<string, ThemeInfo> MergedThemes { get; }


    /// <summary>
    /// 请求下载主题
    /// </summary>
    /// <param name="id">要下载的主题id</param>
    public void RequestDownloadTheme(string id);

    /// <summary>
    /// 请求重启事件
    /// </summary>
    public event EventHandler? RestartRequested;

    /// <summary>
    /// 重载本地主题源
    /// </summary>
    public void LoadThemeSource();

    /// <summary>
    /// 打包主题
    /// </summary>
    /// <param name="id">主题 ID</param>
    /// <param name="outputPath">输出路径</param>
    /// <returns></returns>
    Task PackageThemeAsync(string id, string outputPath);
}