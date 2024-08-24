using System.Windows.Media;

namespace ClassIsland.Core.Models.Theming;

public class ThemeUpdatedEventArgs
{
    public int ThemeMode = 0;
    public Color Primary;
    public Color Secondary;
    public int RealThemeMode = 0;
}