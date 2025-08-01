using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Action;
using Microsoft.Extensions.DependencyInjection;
namespace ClassIsland.Core.Abstractions.Automation;

/// <summary>
/// 行动提供方基类。
/// </summary>
public abstract class ActionBase
{
    /// <summary>
    /// 当此行动触发时，调用此方法。
    /// </summary>
    /// <seealso cref="IsRevertable"/>
    /// <seealso cref="CancellationToken"/>
    /// <seealso cref="Progress"/>
    protected virtual async Task OnInvoke() { }

    /// <summary>
    /// 当此行动恢复时，调用此方法。
    /// 如果此行动没有恢复，则不用实现此方法。
    /// </summary>
    /// <seealso cref="CancellationToken"/>
    /// <seealso cref="Progress"/>
    protected virtual async Task OnRevert() { }

    /// <summary>
    /// 在触发行动时查询此行动项是否将会被恢复。
    /// 例：如果会被恢复，修改应用设置的行动就需要创建临时设置叠层。
    /// </summary>
    protected bool IsRevertable { get; private set; } = false;

    /// <summary>
    /// 代表行动被取消的 <see cref="CancellationToken"/>。
    /// </summary>
    protected CancellationToken? CancellationToken => CancellationTokenSource?.Token;

    /// <summary>
    /// 用于报告行动进度的 <see cref="IProgress{T}"/>。
    /// </summary>
    protected IProgress<double>? Progress { get; private set; }



    /// <summary>
    /// 触发行动。此方法已管理行动项执行生命周期。行动错误会抛出。
    /// </summary>
    /// <param name="actionItem">要触发的行动项。</param>
    /// <param name="isRevertable">行动是否将会被恢复。</param>
    /// <param name="cancellationTokenSource">用于报告行动取消的 <see cref="CancellationTokenSource"/>。通常与所在的行动组共享。</param>
    public async Task InvokeAsync(ActionItem actionItem, bool isRevertable = false, CancellationTokenSource? cancellationTokenSource = null)
    {
        Cancel();
        actionItem.SetStartRunning(cancellationTokenSource);
        IsRevertable = isRevertable;
        SettingsInternal = actionItem.Settings;
        CancellationTokenSource = actionItem.CancellationTokenSource ?? new();
        Progress = new Progress<double>(report => actionItem.Progress = report);
        try
        {
            await OnInvoke();
        }
        catch (Exception ex)
        {
            actionItem.Exception = ex;
            throw;
        }
        finally
        {
            Cancel();
            actionItem.SetEndRunning();
        }
    }

    /// <summary>
    /// 恢复行动。此方法已管理行动项执行生命周期。行动错误会抛出。
    /// </summary>
    /// <param name="actionItem">要触发的行动项。</param>
    /// <param name="cancellationTokenSource">用于报告行动取消的 <see cref="CancellationTokenSource"/>。通常与所在的行动组共享。</param>
    public async Task RevertAsync(ActionItem actionItem, CancellationTokenSource? cancellationTokenSource = null)
    {
        Cancel();
        actionItem.SetStartRunning(cancellationTokenSource);
        SettingsInternal = actionItem.Settings;
        CancellationTokenSource = actionItem.CancellationTokenSource ?? new();
        Progress = new Progress<double>(report => actionItem.Progress = report);
        try
        {
            await OnRevert();
        }
        catch (Exception ex)
        {
            actionItem.Exception = ex;
            throw;
        }
        finally
        {
            Cancel();
            actionItem.SetEndRunning();
        }
    }

    /// <summary>
    /// 取消行动。
    /// </summary>
    public void Cancel()
    {
        CancellationTokenSource?.Cancel();
        CancellationTokenSource = null;
        SettingsInternal = null;
        IsRevertable = false;
        Progress = null;
    }



    /// <summary>
    /// 获取行动提供方实例。
    /// </summary>
    /// <param name="actionItem">要获取行动提供方的行动项。</param>
    public static ActionBase? GetInstance(ActionItem actionItem)
    {
        var provider = IAppHost.Host?.Services.GetKeyedService<ActionBase>(actionItem.Id);
        if (provider == null) return null;

        var settingsType = provider.GetType().BaseType?.GetGenericArguments().FirstOrDefault();
        if (settingsType != null)
        {
            if (actionItem.Settings is JsonElement json)
                actionItem.Settings = json.Deserialize(settingsType);
            if (actionItem.Settings?.GetType() != settingsType)
                actionItem.Settings = Activator.CreateInstance(settingsType);
            provider.SettingsInternal = actionItem.Settings;
        }
        return provider;
    }



    [NotNull] internal object? SettingsInternal { get; set; }

    CancellationTokenSource? CancellationTokenSource { get; set; }
}

/// <inheritdoc cref="ActionBase"/>
public abstract class ActionBase<T> : ActionBase where T : class
{
    /// <summary>
    /// 获取行动项的设置。
    /// </summary>
    protected T Settings => (SettingsInternal as T)!;
}