using System.Drawing;

namespace ClassIsland.Models.EventArgs;

public class MousePosChangedEventArgs(Point pos) : System.EventArgs
{
    public Point Pos { get; } = pos;
}