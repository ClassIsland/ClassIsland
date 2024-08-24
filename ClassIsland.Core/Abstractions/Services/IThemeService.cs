using System.Windows.Media;
using ClassIsland.Core.Models.Theming;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 应用主题服务，控制应用的主题外观。
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// 当前主题
    /// </summary>
    public ITheme? CurrentTheme { get; set; }

    /// <summary>
    /// 主题更新事件，当主题更变时会触发此事件。
    /// </summary>
    public event EventHandler<ThemeUpdatedEventArgs>? ThemeUpdated;

    /// <summary>
    /// 当前颜色主题状态
    /// </summary>
    /// <value>
    /// <list>
    /// <li>0 - 浅色</li><br/>
    /// <li>1 - 深色</li>
    ///</list>
    /// </value>
    public int CurrentRealThemeMode { get; set; }

    /// <summary>
    /// 设置主题
    /// </summary>
    /// <param name="themeMode">主题模式</param>
    /// <param name="primary">第一主题色</param>
    /// <param name="secondary">第二主题色</param>
    public void SetTheme(int themeMode, Color primary, Color secondary);
}