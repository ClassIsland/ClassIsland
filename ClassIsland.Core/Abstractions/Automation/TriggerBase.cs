using System.Diagnostics.CodeAnalysis;
using ClassIsland.Core.Models;

namespace ClassIsland.Core.Abstractions.Automation;

/// <summary>
/// 自动化触发器基类。
/// </summary>
public abstract class TriggerBase
{
    [NotNull] internal object? SettingsInternal { get; set; }

    /// <summary>
    /// 触发这个触发器。
    /// </summary>
    protected void Trigger()
    {
        Triggered?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 触发恢复触发器。
    /// </summary>
    protected void TriggerRevert()
    {
        TriggeredRecover?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 当此触发器被加载到工作流上时，调用此方法。
    /// </summary>
    /// <remarks>触发器可以在这个方法调用时加载需要的资源，比如订阅事件处理器等。</remarks>
    public abstract void Loaded();

    /// <summary>
    /// 当此触发器被从工作流上卸载时，调用此方法。
    /// </summary>
    /// <remarks>触发器必须在这个方法调用时释放之前加载的资源，比如取消订阅事件处理器等，否则可能造成内存泄漏。</remarks>
    public abstract void UnLoaded();

    internal event EventHandler? Triggered;

    internal event EventHandler? TriggeredRecover;

    /// <summary>
    /// 此触发器关联的工作流。
    /// </summary>
    [NotNull] public Workflow? AssociatedWorkflow { get; internal set; }
}

/// <inheritdoc cref="TriggerBase"/>
public abstract class TriggerBase<T> : TriggerBase where T : class
{
    /// <summary>
    /// 当前触发器的设置
    /// </summary>
    protected T Settings => (SettingsInternal as T)!;
}