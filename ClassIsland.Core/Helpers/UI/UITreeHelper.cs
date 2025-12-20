using Avalonia;
using Avalonia.Controls;

namespace ClassIsland.Core.Helpers.UI;

public static class UITreeHelper
{
    public static bool HasParent(Control? control, Control? parent)
    {
        StyledElement? c = control;
        while (c != null)
        {
            c = c.Parent;
            if (c == parent)
            {
                return true;
            }
        }

        return false;
    }
}