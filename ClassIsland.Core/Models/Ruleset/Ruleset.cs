using System.Collections.ObjectModel;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Ruleset;

/// <summary>
/// 代表一个包含若干个规则的规则集。要判定规则集是否生效，需要使用<see cref="IRulesetService"/>。
/// </summary>
public class Ruleset : ObservableRecipient
{
    private RulesetLogicalMode _mode = RulesetLogicalMode.Or;
    private bool _isReversed = false;
    private ObservableCollection<Rule> _rules = new ObservableCollection<Rule>();

    /// <summary>
    /// 逻辑模式。
    /// </summary>
    public RulesetLogicalMode Mode
    {
        get => _mode;
        set
        {
            if (value == _mode) return;
            _mode = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否反转判断。
    /// </summary>
    public bool IsReversed
    {
        get => _isReversed;
        set
        {
            if (value == _isReversed) return;
            _isReversed = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 规则条目。
    /// </summary>
    public ObservableCollection<Rule> Rules
    {
        get => _rules;
        set
        {
            if (Equals(value, _rules)) return;
            _rules = value;
            OnPropertyChanged();
        }
    }
}