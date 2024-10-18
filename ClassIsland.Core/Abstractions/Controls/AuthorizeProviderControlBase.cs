using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;

namespace ClassIsland.Core.Abstractions.Controls;

/// <summary>
/// 认证提供方控件基类。
/// </summary>
public abstract class AuthorizeProviderControlBase : UserControl
{
    /// <summary>
    /// 通过认证。
    /// </summary>
    protected void CompleteAuthorize()
    {

    }

    /// <summary>
    /// 当前认证提供方控件的设置
    /// </summary>
    [NotNull]
    internal object? SettingsInternal { get; set; }
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