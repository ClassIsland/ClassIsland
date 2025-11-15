using System.Text.Json;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Automation;
using Microsoft.Extensions.DependencyInjection;
namespace ClassIsland.Core.Abstractions.Automation;

/// <summary>
/// 行动提供方基类。
/// </summary>
public abstract class ActionBase
{
    /// 当此行动触发时，此方法将被调用。
    /// <remarks>
    /// 重写本方法时，请先调用基类的实现 <code>base.OnInvoke();</code>
    /// </remarks>
    /// <seealso cref="ActionBase{TSettings}.Settings"/>
    /// <seealso cref="IsRevertable"/>
    /// <seealso cref="InterruptCancellationToken"/>
    protected virtual async Task OnInvoke()
    {
        ActionItem.SetStartRunning();
    }

    /// 当此行动恢复时，此方法将被调用。<br/>
    /// 重要：如果此行动提供方没有恢复行动，请勿重写此方法。
    /// <remarks>
    /// 重写本方法时，请先调用基类的实现 <code>base.OnRevert();</code>
    /// </remarks>
    /// <seealso cref="ActionBase{TSettings}.Settings"/>
    /// <seealso cref="InterruptCancellationToken"/>
    protected virtual async Task OnRevert()
    {
        ActionItem.SetStartRunning();
    }

    /// 当此行动运行被中断时，此方法将被调用。
    /// <remarks>
    /// 此方法与 <see cref="InterruptCancellationToken"/> 等效。<br/>
    /// 如果此行动是瞬间完成的，则不必实现此方法。
    /// </remarks>
    protected virtual async Task OnInterrupted() { }



    /// 在触发行动时查询此行动项是否将会被恢复。
    /// 在恢复行动时，此属性始终为 false。
    protected bool IsRevertable { get; private set; }

    /// 表示行动运行被中断的 <see cref="CancellationToken"/>。
    protected CancellationToken InterruptCancellationToken => InterruptCts.Token;

    /// 此行动所运行的行动项。
    /// <seealso cref="ActionItem.Progress"/>
    protected ActionItem ActionItem { get; private set; }

    /// 该行动项所在的行动组。
    /// <seealso cref="ActionSet.Guid"/>
    protected ActionSet ActionSet { get; private set; }



    /// 触发行动。此方法已管理行动项运行生命周期。行动错误会抛出。
    /// <param name="actionItem">要触发的行动项。</param>
    /// <param name="actionSet">行动项所在的行动组。</param>
    /// <param name="isRevertable">行动是否将会被恢复。</param>
    public async Task InvokeAsync(ActionItem actionItem, ActionSet actionSet, bool isRevertable = true)
    {
        await ExecuteAsync(actionItem, actionSet, isRevertable, OnInvoke);
    }

    /// 恢复行动。此方法已管理行动项运行生命周期。行动错误会抛出。
    /// <param name="actionItem">要触发的行动项。</param>
    /// <param name="actionSet">行动项所在的行动组。</param>
    public async Task RevertAsync(ActionItem actionItem, ActionSet actionSet)
    {
        await ExecuteAsync(actionItem, actionSet, false, OnRevert);
    }

    async Task ExecuteAsync(ActionItem actionItem, ActionSet actionSet, bool isRevertable, Func<Task> action)
    {
        ActionItem = actionItem;
        ActionSet = actionSet;
        IsRevertable = isRevertable;
        SettingsInternal = actionItem.Settings;
        InterruptCts = actionSet.InterruptCts ?? throw new ArgumentNullException(nameof(actionSet.InterruptCts));
        var cancellationRegistration = InterruptCancellationToken.Register(async () => { await OnInterrupted(); });

        try
        {
            await action();
        }
        catch (Exception ex) when (ex is TaskCanceledException && InterruptCts?.IsCancellationRequested == true) { }
        catch (Exception ex)
        {
            actionItem.Exception = ex.ToString();
            throw;
        }
        finally
        {
            await cancellationRegistration.DisposeAsync();
            actionItem.SetEndRunning();
        }
    }



    internal object? SettingsInternal { get; set; }
    internal CancellationTokenSource InterruptCts { get; set; }

    static Lazy<IActionService> ActionService = new(IAppHost.GetService<IActionService>());

    /// 获取行动提供方实例。
    /// <param name="actionItem">要获取行动提供方的行动项。</param>
    public static ActionBase? GetInstance(ActionItem? actionItem)
    {
        if (string.IsNullOrEmpty(actionItem?.Id)) return null;

        var provider = IAppHost.Host?.Services.GetKeyedService<ActionBase>(actionItem.Id);
        if (provider == null)
        {
            ActionService.Value.MigrateUnknownActionItem(actionItem);
            provider = IAppHost.Host?.Services.GetKeyedService<ActionBase>(actionItem.Id);
            if (provider == null) return null;
        }

        var settingsType = provider.GetType().BaseType?.GetGenericArguments().FirstOrDefault();
        if (settingsType != null)
        {
            if (actionItem.Settings is JsonElement json)
                actionItem.Settings = json.Deserialize(settingsType);
            if (actionItem.Settings?.GetType() != settingsType)
                actionItem.Settings = Activator.CreateInstance(settingsType);
        }
        provider.SettingsInternal = actionItem.Settings;
        return provider;
    }
}

/// <inheritdoc cref="ActionBase"/>
/// <typeparam name="TSettings">行动设置类型。需要获取行动设置的行动提供方须标注此类型。</typeparam>
public abstract class ActionBase<TSettings> : ActionBase where TSettings : class
{
    /// 当前行动项的设置。
    /// 在行动运行期间，用户更改会实时更新。
    protected TSettings Settings => (SettingsInternal as TSettings)!;
}