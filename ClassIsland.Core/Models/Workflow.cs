using System.Collections.ObjectModel;
using ClassIsland.Core.Models.Action;
using ClassIsland.Shared.Models.Action;
using CommunityToolkit.Mvvm.ComponentModel;
namespace ClassIsland.Core.Models;

/// <summary>
/// 代表一个自动化工作流。
/// </summary>
public partial class Workflow : ObservableRecipient
{
    /// <summary>
    /// 触发器。
    /// </summary>
    [ObservableProperty] ObservableCollection<TriggerSettings> _triggers = [];

    /// <summary>
    /// 是否启用规则集。
    /// </summary>
    [ObservableProperty] bool _isConditionEnabled = false;

    /// <summary>
    /// 规则集。
    /// </summary>
    [ObservableProperty] Ruleset.Ruleset _ruleset = new();

    /// <summary>
    /// 行动组。
    /// </summary>
    [ObservableProperty] ActionSet _actionSet = new();

    internal void Unload() => Unloading?.Invoke(this, EventArgs.Empty);
    internal event EventHandler? Unloading;
}