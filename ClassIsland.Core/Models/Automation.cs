using ClassIsland.Core.Models.Action;
using CommunityToolkit.Mvvm.ComponentModel;
namespace ClassIsland.Core.Models;

/// <summary>
/// 代表一个自动化。自动化会被自动触发和恢复。
/// </summary>
public class Automation : ObservableRecipient
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

    private Actionset _actionset = new();
    /// <summary>
    /// 行动组
    /// </summary>
    public Actionset Actionset
    {
        get => _actionset;
        set
        {
            if (value == _actionset) return;
            _actionset = value;
            OnPropertyChanged();
        }
    }
}