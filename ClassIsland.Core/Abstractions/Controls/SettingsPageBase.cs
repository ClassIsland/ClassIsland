using System.Windows.Controls;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Core.Abstractions.Controls;

/// <summary>
/// 设置页面基类
/// </summary>
public abstract class SettingsPageBase : Page
{
    /// <summary>
    /// 设置窗口的<see cref="DialogHost"/>的标识符。
    /// </summary>
    public static readonly string DialogHostIdentifier = "SettingsWindowV2";

    /// <summary>
    /// 打开设置窗口抽屉命令
    /// </summary>
    public static readonly ICommand OpenDrawerCommand = new RoutedCommand();

    /// <summary>
    /// 关闭设置窗口抽屉命令
    /// </summary>
    public static readonly ICommand CloseDrawerCommand = new RoutedCommand();

    /// <summary>
    /// 请求重启应用命令
    /// </summary>
    public static readonly ICommand RequestRestartCommand = new RoutedCommand();

    /// <summary>
    /// 在设置窗口打开指定的抽屉。
    /// </summary>
    /// <param name="key">抽屉资源键名</param>
    protected void OpenDrawer(string key)
    {
        OpenDrawer(FindResource(key));
    }

    /// <summary>
    /// 在设置窗口打开指定的抽屉。
    /// </summary>
    /// <param name="o">要在抽屉中显示的对象</param>
    protected void OpenDrawer(object o)
    {
        OpenDrawerCommand.Execute(o);
    }

    /// <summary>
    /// 关闭设置窗口抽屉。
    /// </summary>
    protected void CloseDrawer()
    {
        CloseDrawerCommand.Execute(null);
    }

    /// <summary>
    /// 请求重启应用。
    /// </summary>
    protected void RequestRestart()
    {
        RequestRestartCommand.Execute(null);
    }
}