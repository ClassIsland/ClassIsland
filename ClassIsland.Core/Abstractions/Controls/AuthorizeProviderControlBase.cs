using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Abstractions.Controls;

/// <summary>
/// 认证提供方控件基类。
/// </summary>
public abstract class AuthorizeProviderControlBase : UserControl
{
    /// <summary>
    /// 完成认证命令。
    /// </summary>
    public static readonly ICommand CompleteAuthorizeCommand = new RoutedUICommand();

    /// <summary>
    /// 通过认证。
    /// </summary>
    protected void CompleteAuthorize()
    {
        CompleteAuthorizeCommand.Execute(null);
    }

    /// <summary>
    /// 当前认证提供方控件的设置
    /// </summary>
    [NotNull]
    internal object? SettingsInternal { get; set; }

    /// <summary>
    /// 从设置对象获取控件实例。
    /// </summary>
    /// <param name="info">控件信息</param>
    /// <param name="settings">要附加的设置对象</param>
    /// <returns>初始化的控件对象。</returns>
    public static AuthorizeProviderControlBase? GetInstance(AuthorizeProviderInfo info, ref object? settings, bool isEditngMode)
    {
        var control = IAppHost.Host?.Services.GetKeyedService<AuthorizeProviderControlBase>(info.Id);
        if (control == null)
        {
            return null;
        }

        var baseType = info.AuthorizeProviderType?.BaseType;
        if (baseType?.GetGenericArguments().Length > 0)
        {
            var settingsType = baseType.GetGenericArguments().First();
            var settingsReal = settings ?? Activator.CreateInstance(settingsType);
            if (settingsReal is JsonElement json)
            {
                settingsReal = json.Deserialize(settingsType);
            }
            settings = settingsReal;

            control.SettingsInternal = settingsReal;
        }

        control.IsEditingMode = isEditngMode;
        return control;
    }

    /// <summary>
    /// 当前认证提供方是否处于编辑模式
    /// </summary>
    public bool IsEditingMode { get; private set; }
}

/// <summary>
/// 认证提供方控件基类。
/// </summary>

public abstract class AuthorizeProviderControlBase<T> : AuthorizeProviderControlBase where T : class
{
    /// <summary>
    /// 当前组件的设置
    /// </summary>
    public T Settings => (SettingsInternal as T)!;

}