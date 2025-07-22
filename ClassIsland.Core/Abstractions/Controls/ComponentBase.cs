using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Avalonia.Controls;
using ClassIsland.Shared;
using Microsoft.Extensions.Logging;

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

    /// <summary>
    /// 当这个组件是由另一个组件迁移而来时触发。
    /// </summary>
    /// <param name="sourceId">源组件 GUID</param>
    /// <param name="settings">源组件的设置</param>
    public virtual void OnMigrated(Guid sourceId, object? settings)
    {
    }
}

/// <summary>
/// 主界面组件基类
/// </summary>
public abstract class ComponentBase<T> : ComponentBase where T : class
{
    /// <summary>
    /// 当前组件的设置
    /// </summary>
    public T Settings => (SettingsInternal as T)!;
}