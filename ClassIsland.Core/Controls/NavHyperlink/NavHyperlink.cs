using System.Windows;
using System.Windows.Documents;
using ClassIsland.Core.Commands;

namespace ClassIsland.Core.Controls.NavHyperlink;

/// <summary>
/// 可调用应用 Uri 导航的 <see cref="Hyperlink"/>
/// </summary>
public class NavHyperlink : Hyperlink
{
    public static readonly DependencyProperty NavigateTargetProperty = DependencyProperty.Register(
        nameof(NavigateTarget), typeof(string), typeof(NavHyperlink), new PropertyMetadata(default(Uri), (o, args) =>
        {
            if (o is not NavHyperlink link)
            {
                return;
            }

            link.CommandParameter = link.NavigateTarget;
        }));

    public string NavigateTarget
    {
        get { return (string)GetValue(NavigateTargetProperty); }
        set { SetValue(NavigateTargetProperty, value); }
    }

    /// <inheritdoc />
    public NavHyperlink()
    {
        Command = UriNavigationCommands.UriNavigationCommand;
    }
}