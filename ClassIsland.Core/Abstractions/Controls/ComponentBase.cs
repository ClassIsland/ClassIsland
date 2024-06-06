using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;

namespace ClassIsland.Core.Abstractions.Controls;

/// <summary>
/// 主界面组件基类
/// </summary>
public abstract class ComponentBase : UserControl
{
    /// <summary>
    /// 当前组件的设置
    /// </summary>
    [NotNull]
    internal object? SettingsInternal { get; set; }
}

/// <summary>
/// 主界面组件基类
/// </summary>
public abstract class ComponentBase<T> : ComponentBase where T : class
{
    /// <summary>
    /// 当前组件的设置
    /// </summary>
    internal object? Settings => SettingsInternal as T;
}