using System.Collections.ObjectModel;
using ClassIsland.Core.Models.Action;
using ClassIsland.Shared.Models.Action;
using CommunityToolkit.Mvvm.ComponentModel;
namespace ClassIsland.Core.Models;

/// <summary>
/// 代表一个自动化工作流。自动化工作流会被自动触发和恢复。
/// </summary>
public class Workflow : ObservableRecipient
{
    private Ruleset.Ruleset _ruleset = new();
    /// <summary>
    /// 规则集
    /// </summary>
    public Ruleset.Ruleset Ruleset
    {
        get => _ruleset;
        set
        {
            if (value == _ruleset) return;
            _ruleset = value;
            OnPropertyChanged();
        }
    }

    private ActionSet _actionSet = new();
    private ObservableCollection<TriggerSettings> _triggers = [];
    private bool _isConditionEnabled = false;


    /// <summary>
    /// 行动组
    /// </summary>
    public ActionSet ActionSet
    {
        get => _actionSet;
        set
        {
            if (value == _actionSet) return;
            _actionSet = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 触发此工作流的触发器
    /// </summary>
    public ObservableCollection<TriggerSettings> Triggers
    {
        get => _triggers;
        set
        {
            if (Equals(value, _triggers)) return;
            _triggers = value;
            OnPropertyChanged();
        }
    }

    internal void Unload()
    {
        Unloading?.Invoke(this, EventArgs.Empty);
    }

    internal event EventHandler? Unloading;

    /// <summary>
    /// 是否启用条件判定
    /// </summary>
    public bool IsConditionEnabled
    {
        get => _isConditionEnabled;
        set
        {
            if (value == _isConditionEnabled) return;
            _isConditionEnabled = value;
            OnPropertyChanged();
        }
    }
}