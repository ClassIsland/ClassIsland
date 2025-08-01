using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Action;
using Microsoft.Extensions.DependencyInjection;
namespace ClassIsland.Core.Abstractions.Controls;

/// <summary>
/// 行动提供方设置控件基类。
/// </summary>
public abstract class ActionSettingsControlBase : UserControl
{
    [NotNull] internal object? SettingsInternal { get; set; }

    /// <summary>
    /// 获取行动提供方设置控件实例。
    /// </summary>
    /// <param name="actionItem">要获取行动提供方的行动项。</param>
    public static ActionSettingsControlBase? GetInstance(ActionItem actionItem)
    {
        var control = IAppHost.Host?.Services.GetKeyedService<ActionSettingsControlBase>(actionItem.Id);
        if (control == null) return null;

        var settingsType = control.GetType().BaseType?.GetGenericArguments().FirstOrDefault();
        if (settingsType != null)
        {
            if (actionItem.Settings is JsonElement json)
                actionItem.Settings = json.Deserialize(settingsType);
            if (actionItem.Settings?.GetType() != settingsType)
                actionItem.Settings = Activator.CreateInstance(settingsType);
            control.SettingsInternal = actionItem.Settings;
        }
        return control;
    }
}

/// <inheritdoc />
public abstract class ActionSettingsControlBase<T> : ActionSettingsControlBase where T : class
{
    /// <summary>
    /// 当前行动项的设置。
    /// </summary>
    public T Settings => (SettingsInternal as T)!;
}
