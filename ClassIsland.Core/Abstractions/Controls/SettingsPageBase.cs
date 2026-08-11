using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Labs.Input;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Abstractions.Controls;

/// <summary>
/// 设置页面基类
/// </summary>
public abstract class SettingsPageBase : UserControl
{
    /// <summary>
    /// 设置窗口的<see cref="DialogHost"/>的标识符。
    /// </summary>
    public static readonly string DialogHostIdentifier = "SettingsWindowV2";

    /// <summary>
    /// 打开设置窗口抽屉命令
    /// </summary>
    public static readonly ICommand OpenDrawerCommand = new RoutedCommand(nameof(OpenDrawerCommand));

    /// <summary>
    /// 关闭设置窗口抽屉命令
    /// </summary>
    public static readonly ICommand CloseDrawerCommand = new RoutedCommand(nameof(CloseDrawerCommand));

    /// <summary>
    /// 请求重启应用命令
    /// </summary>
    public static readonly ICommand RequestRestartCommand = new RoutedCommand(nameof(RequestRestartCommand));

    /// <summary>
    /// 在设置窗口打开指定的抽屉。
    /// </summary>
    /// <param name="key">抽屉资源键名</param>
    /// <param name="useGlobalDataContext">是否使用设置界面的数据上下文，为false时则使用本页面的数据上下文。</param>
    /// <param name="dataContext">抽屉元素的数据上下文</param>
    protected void OpenDrawer(string key, bool useGlobalDataContext=false, object? dataContext=null)
    {
        OpenDrawer(this.FindResource(key)!, useGlobalDataContext,  dataContext);
    }

    /// <summary>
    /// 在设置窗口打开指定的抽屉。
    /// </summary>
    /// <param name="o">要在抽屉中显示的对象</param>
    /// <param name="useGlobalDataContext">是否使用设置界面的数据上下文，为false时则使用本页面的数据上下文。</param>
    /// <param name="dataContext">抽屉元素的数据上下文</param>
    protected void OpenDrawer(object o, bool useGlobalDataContext=false, object? dataContext=null)
    {
        if (o is Control e && !useGlobalDataContext)
        {
            e.DataContext = dataContext ?? this;
        }
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
    
    /// <summary>
    /// 导航到本设置页面时使用的 Uri（如有）
    /// </summary>
    public Uri? NavigationUri { get; internal set; }

    /// <summary>
    /// 恢复默认设置时需要重置的设置属性名列表。
    /// 返回空列表表示本页面不提供"恢复默认设置"按钮。
    /// 支持子对象属性名，恢复时会将整个子对象替换为默认值。
    /// </summary>
    public virtual IReadOnlyList<string> GetSettingsResetTargetProperties() => [];

    /// <summary>
    /// 将当前设置恢复为默认值。
    /// </summary>
    /// <param name="currentSettings">当前设置对象</param>
    /// <param name="defaultSettings">默认值来源（新创建的默认实例）</param>
    public virtual void ResetSettingsToDefault(object currentSettings, object defaultSettings)
    {
        var type = currentSettings.GetType();
        foreach (var name in GetSettingsResetTargetProperties())
        {
            var prop = type.GetProperty(name);
            if (prop is not { CanWrite: true } || prop.GetIndexParameters().Length > 0) continue;
            prop.SetValue(currentSettings, prop.GetValue(defaultSettings));
        }
    }
}
