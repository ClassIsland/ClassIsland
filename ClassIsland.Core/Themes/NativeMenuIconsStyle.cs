using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using ClassIsland.Core.Assists;

namespace ClassIsland.Core.Themes;

public class NativeMenuIconsStyle : Styles
{
    private static Selector NativeMenuItemPresenterSelector { get; } = Selectors.Is(null,
        typeof(MenuItem));
    
    public NativeMenuIconsStyle()
    {
        var style = new Style()
        {
            Selector = NativeMenuItemPresenterSelector,
            Setters =
            {
                new Setter(NativeMenuItemAssist.OverrideIconProperty, true)
            }
        };
        Add(style);
    }
}