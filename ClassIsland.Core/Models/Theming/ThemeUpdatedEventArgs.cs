using Avalonia.Media;

namespace ClassIsland.Core.Models.Theming;

public class ThemeUpdatedEventArgs
{
    /// <summary>
    /// 主题模式。0 - 跟随系统，1 - 浅色，2 - 深色。
    /// </summary>
    public int ThemeMode = 0;

    /// <summary>
    /// 当前生效的主强调色。
    /// </summary>
    public Color Primary;

    /// <summary>
    /// 次强调色。目前未使用，始终为默认值，请勿依赖此字段。
    /// </summary>
    public Color Secondary;

    /// <summary>
    /// 当前实际生效的外观。0 - 浅色，1 - 深色。跟随系统时表示系统当前的外观。
    /// </summary>
    public int RealThemeMode = 0;
}
