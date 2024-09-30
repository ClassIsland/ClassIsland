using ClassIsland.Core.Models.Action;
using CommunityToolkit.Mvvm.ComponentModel;
namespace ClassIsland.Core.Models;

/// <summary>
/// 代表一个规则行动组。
/// </summary>
public class RuleActionPair : ObservableRecipient
{
    private bool _isEnabled = true;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (value == _isEnabled) return;
            _isEnabled = value;
            OnPropertyChanged();
        }
    }

    private string _name = "";
    /// <summary>
    /// 名称
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (value == _name) return;
            _name = value;
            OnPropertyChanged();
        }
    }

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

    private ActionList _actionList = new();
    /// <summary>
    /// 行动列表
    /// </summary>
    public ActionList ActionList
    {
        get => _actionList;
        set
        {
            if (value == _actionList) return;
            _actionList = value;
            OnPropertyChanged();
        }
    }
}