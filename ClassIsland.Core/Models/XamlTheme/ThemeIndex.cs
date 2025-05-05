using System.Collections.ObjectModel;
using ClassIsland.Core.Models.Plugin;

namespace ClassIsland.Core.Models.XamlTheme;

/// <summary>
/// 主题仓库索引
/// </summary>
public class ThemeIndex
{
    /// <summary>
    /// 主题仓库包含的主题列表
    /// </summary>
    public ObservableCollection<ThemeIndexItem> Themes { get; set; } = [];
}