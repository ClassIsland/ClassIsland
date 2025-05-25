using System.Windows.Input;

namespace ClassIsland.Core.Commands;


/// <summary>
/// Uri 导航相关命令。
/// </summary>
public class UriNavigationCommands
{
    /// <summary>
    /// Uri 导航命令。
    /// </summary>
    public static readonly ICommand UriNavigationCommand = new RoutedUICommand();
}